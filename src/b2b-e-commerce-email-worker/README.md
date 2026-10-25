# Email Worker — B2B E-Commerce

Servicio en segundo plano desarrollado con **.NET 10** que consume mensajes de **Amazon SQS** y envía correos electrónicos por **SMTP**. En desarrollo se utilizan **LocalStack** para emular SQS y **Mailpit** para recibir los correos sin enviarlos a direcciones reales.

## Flujo de funcionamiento

```text
Productor de mensajes (manual durante las pruebas; API en una integración futura)
    → SQS: cola-correos (LocalStack)
    → Email Worker (.NET)
    → SMTP: Mailpit
    → http://localhost:8025
```

Si un mensaje falla repetidamente, SQS puede trasladarlo a `cola-correos-dlq` después de superar el límite configurado de recepciones. El Worker solo elimina los mensajes que procesa correctamente.

## Requisitos

- SDK de **.NET 10**.
- **Docker Desktop** con Docker Compose disponible.
- Un token válido de LocalStack para la imagen utilizada por el proyecto.
- PowerShell para seguir los ejemplos (pueden adaptarse a otras terminales).

## Configuración inicial

Ejecutar los siguientes comandos desde la **raíz del repositorio**, donde se encuentra `docker-compose.yaml`.

1. Crear el archivo local de variables de entorno:

   ```powershell
   Copy-Item .env.example .env
   ```

2. Abrir `.env` y reemplazar el valor de ejemplo por el token propio:

   ```dotenv
   LOCALSTACK_AUTH_TOKEN=tu-token-de-localstack
   ```

   **Nunca subir `.env` ni credenciales reales a Git.** El archivo `.env.example` sí debe mantenerse versionado.

3. Revisar la configuración del Worker en `src/b2b-e-commerce-email-worker/appSettings.yaml.token`:

   ```yaml
   AWS:
     ServiceURL: "http://localhost:4567"
     Region: "us-east-1"
     QueueName: "cola-correos"
   SMTP:
     Host: "localhost"
     Port: 1025
     SenderEmail: "no-reply@b2b.local"
     SenderName: "B2B E-Commerce"
     Security: "None"
   ```

   El proyecto ya incluye esta configuración local de ejemplo. Se puede sobreescribir con `appSettings.yaml` o variables de entorno cuando corresponda. Mailpit no requiere usuario ni contraseña. Para un SMTP autenticado, configurar ambos valores (`SMTP:Username` y `SMTP:Password`).

## Levantar los servicios locales

```powershell
docker compose up -d
```

Esto inicia únicamente **LocalStack** y **Mailpit**. **El Worker se ejecuta por separado con .NET**, no dentro de Docker.

El script `docker/localstack/init/01-crear-colas.sh` inicializa las colas:

- `cola-correos`: cola principal; tiempo de visibilidad de 60 segundos.
- `cola-correos-dlq`: cola de mensajes fallidos (DLQ); política de redirección con `maxReceiveCount=3`.

Verificar el estado:

```powershell
docker compose ps
docker compose logs localstack
```

Servicios disponibles:

| Servicio | Dirección local | Uso |
| --- | --- | --- |
| LocalStack / SQS | `http://localhost:4567` | API compatible con SQS |
| Mailpit SMTP | `localhost:1025` | Recepción SMTP de prueba |
| Mailpit web | `http://localhost:8025` | Visualización de correos |

## Compilar y ejecutar el Worker

Desde la raíz del repositorio:

```powershell
dotnet restore .\src\b2b-e-commerce-email-worker\b2b-e-commerce-email-worker.csproj
dotnet build .\src\b2b-e-commerce-email-worker\b2b-e-commerce-email-worker.csproj
dotnet run --project .\src\b2b-e-commerce-email-worker
```

Dejar la terminal abierta mientras se necesite procesar correos. Para detener el Worker, presionar **Ctrl+C**.

## Probar el envío de un correo

Con los contenedores y el Worker ejecutándose, abrir **otra terminal PowerShell** desde la raíz del repositorio.

El siguiente comando envía un mensaje de prueba mediante AWS CLI **dentro del contenedor LocalStack**, por lo que no hace falta instalar AWS CLI en Windows:

```powershell
$mensaje = '{"Destinatario":"prueba@ejemplo.com","Asunto":"Prueba del Email Worker","Cuerpo":"Correo enviado desde SQS y recibido por Mailpit."}'
docker exec b2b-localstack awslocal sqs send-message --queue-url http://sqs.us-east-1.localhost.localstack.cloud:4566/000000000000/cola-correos --message-body $mensaje
```

**Nota:** el comando anterior está pensado para PowerShell; si la versión de Docker/PowerShell altera las comillas del JSON, se puede enviar el cuerpo mediante un archivo JSON o desde otro cliente SQS.

Verificar que el Worker registre la recepción y procesamiento del mensaje. Luego abrir **http://localhost:8025** y comprobar que aparezca el correo de prueba.

El formato del mensaje esperado es:

```json
{
  "Destinatario": "prueba@ejemplo.com",
  "Asunto": "Prueba del Email Worker",
  "Cuerpo": "Contenido del correo"
}
```

Los tres campos son obligatorios.

## Solución de problemas

| Problema | Qué revisar |
| --- | --- |
| Docker Compose indica que falta `LOCALSTACK_AUTH_TOKEN` | Crear `.env` en la raíz y definir un token válido. |
| El Worker no encuentra `cola-correos` | Comprobar `docker compose logs localstack` y que el script de inicialización terminó correctamente. |
| Error al conectar a SQS | Verificar que LocalStack esté activo y que `AWS:ServiceURL` apunte a `http://localhost:4567`. |
| Error al enviar por SMTP | Verificar Mailpit y `SMTP:Host=localhost`, `SMTP:Port=1025`, `SMTP:Security=None`. |
| El correo no aparece en Mailpit | Consultar los logs del Worker; confirmar que el mensaje usa `Destinatario`, `Asunto` y `Cuerpo`. |
| Mensajes repetidamente fallidos | Revisar la cola `cola-correos-dlq` y el error registrado por el Worker. |

## Detener los servicios

```powershell
docker compose down
```

Esto detiene y elimina los contenedores del Compose. Como no se ha configurado almacenamiento persistente para SQS, **no se debe asumir que los mensajes sobreviven a una recreación de LocalStack**.

## Alcance actual

- El Worker consume correos de SQS y los entrega a Mailpit mediante SMTP.
- La publicación automática de mensajes desde la API pertenece a una integración posterior.
- La configuración actual está orientada al desarrollo local; credenciales AWS reales y un servicio de correo de producción requieren configuración adicional.
