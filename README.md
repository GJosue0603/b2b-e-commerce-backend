# B2B E-Commerce — Backend

Backend de una plataforma de comercio electrónico B2B, desarrollado con **C# y .NET 10**. Incluye una API REST, acceso a MongoDB y un servicio independiente (*Email Worker*) que consume mensajes de **Amazon SQS** (emulado localmente con **LocalStack**) para enviar correos electrónicos.

> **Objetivo de este README:** permitir que un nuevo integrante clone el repositorio, configure su entorno local y ejecute la API, LocalStack y el Email Worker sin depender de configuraciones privadas de otros desarrolladores.

## Contenido

1. [Tecnologías y arquitectura](#tecnologías-y-arquitectura)
2. [Requisitos previos](#requisitos-previos)
3. [Clonar y preparar el proyecto](#clonar-y-preparar-el-proyecto)
4. [Configurar archivos locales](#configurar-archivos-locales)
5. [Levantar LocalStack con Docker](#levantar-localstack-con-docker)
6. [Verificar las colas SQS](#verificar-las-colas-sqs)
7. [Ejecutar la API y el Email Worker](#ejecutar-la-api-y-el-email-worker)
8. [Probar el flujo de registro](#probar-el-flujo-de-registro)
9. [Solución de problemas](#solución-de-problemas)
10. [Guía de contribución](#guía-de-contribución)

## Tecnologías y arquitectura

| Componente | Tecnología | Función |
| --- | --- | --- |
| API REST | ASP.NET Core / .NET 10 | Endpoints, autenticación JWT y lógica HTTP |
| Lógica de negocio | C# / .NET 10 | Validaciones y reglas de negocio |
| Persistencia | MongoDB | Usuarios, registros pendientes y demás entidades |
| Mensajería | Amazon SQS mediante LocalStack | Cola de solicitudes de correo |
| Email Worker | .NET Worker Service | Consumo de SQS y envío por SMTP |
| Envío de correos | SMTP (configuración actual: Gmail) | Entrega de mensajes y códigos de verificación |
| Contenedores | Docker Desktop + Docker Compose | Ejecución local de LocalStack |

### Estructura principal

```text
b2b-e-commerce-backend/
├── b2b-e-commerce-backend.sln
├── docker-compose.yaml
├── env.example
├── .editorconfig
├── docker/
│   └── localstack/
│       └── init/
│           └── 01-crear-colas.sh
└── src/
    ├── b2b-e-commerce-api/             # API REST, controladores y DI
    ├── b2b-e-commerce-bll/             # Lógica de negocio y publicación SQS
    ├── b2b-e-commerce-repository/      # Acceso a MongoDB
    ├── b2b-e-commerce-models/          # Modelos y DTOs
    ├── b2b-e-commerce-utilities/       # Utilidades y excepciones
    └── b2b-e-commerce-email-worker/    # Consumidor SQS y servicio SMTP
```

### Flujo de correos

```text
Cliente → API → MongoDB (registro pendiente)
                ↓
              SQS (cola-correos, LocalStack)
                ↓
           Email Worker
                ↓
           Servidor SMTP
                ↓
           Correo al usuario
```

La cola `cola-correos-dlq` recibe mensajes que no se pueden procesar después de los reintentos configurados. **La API y el Worker son procesos separados**; ejecutar solo la API no envía los correos por sí mismo.

## Requisitos previos

- **Git** y acceso al repositorio.
- **SDK de .NET 10** (`dotnet --version`).
- **Docker Desktop** instalado y **en ejecución**, con Docker Compose (`docker compose version`).
- Editor como Visual Studio o VS Code con extensiones de C#.
- Acceso a la instancia de **MongoDB** autorizada para desarrollo (por ejemplo, MongoDB Atlas).
- **Token personal de LocalStack** compatible con la imagen configurada en el proyecto.
- Configuración SMTP válida para el Email Worker. Para Gmail, solicitar al responsable del proyecto el procedimiento aprobado de acceso; **no compartir contraseñas en Git ni por canales públicos**.

Los comandos siguientes están escritos para **PowerShell en Windows**, desde la raíz del repositorio, salvo indicación contraria.

## Clonar y preparar el proyecto

```powershell
git clone https://github.com/GJosue0603/b2b-e-commerce-backend.git
cd b2b-e-commerce-backend

dotnet --version
docker compose version
dotnet restore .\b2b-e-commerce-backend.sln
dotnet build .\b2b-e-commerce-backend.sln
```

Si usas SSH, puedes clonar con `git@github.com:GJosue0603/b2b-e-commerce-backend.git` siempre que tengas la clave y los permisos configurados.

> La compilación puede completarse antes de configurar MongoDB y SMTP; **ejecutar** los servicios sí requiere sus valores de configuración.

## Configurar archivos locales

**Antes de iniciar cualquier servicio**, configura los siguientes archivos:

| Archivo | Ubicación | Para qué sirve |
| --- | --- | --- |
| `.env` | Raíz del repositorio | Token de LocalStack utilizado por Docker Compose |
| `appSettings.yaml` | `src/b2b-e-commerce-api/` | JWT, MongoDB y conexión SQS de la API |
| `appSettings.yaml` | `src/b2b-e-commerce-email-worker/` | Conexión SQS y configuración SMTP del Worker |

**Los dos `appSettings.yaml` son distintos y ambos son necesarios** para el flujo completo de correos. Solicita los valores autorizados al responsable del proyecto; no copies credenciales reales de producción a tu entorno personal.

### 1. Crear `.env`

Desde la raíz:

```powershell
Copy-Item .\env.example .\.env
```

Edita `.env`:

```dotenv
LOCALSTACK_AUTH_TOKEN=TU_TOKEN_PERSONAL_DE_LOCALSTACK
```

El archivo `env.example` está versionado; `.env` está excluido por `.gitignore` y **no debe subirse**.

### 2. Configurar la API

El repositorio contiene una plantilla `src/b2b-e-commerce-api/appSettings.yaml.token` con marcadores de sustitución. Crea tu archivo local:

```powershell
Copy-Item .\src\b2b-e-commerce-api\appSettings.yaml.token .\src\b2b-e-commerce-api\appSettings.yaml
```

Reemplaza los marcadores con **valores locales autorizados**. Ejemplo de estructura (valores ilustrativos):

```yaml
App:
  Nombre: "B2B E-Commerce API"
  Environment: "Development"

Jwt:
  Secreto: "REEMPLAZAR_CON_SECRETO_ALEATORIO_DE_AL_MENOS_32_BYTES"
  ExpiracionEnMinutos: 60
  Emisor: "b2b-e-commerce-api"
  Audiencia: "b2b-e-commerce-frontend"

Mongo:
  ConnectionString: "mongodb+srv://USUARIO:CONTRASENA@CLUSTER/EJEMPLO"
  Database: "NOMBRE_BASE_DE_DATOS_DEV"

AWS:
  ServiceURL: "http://localhost:4567"
  Region: "us-east-1"
  QueueName: "cola-correos"
```

**Importante:** el secreto JWT debe ser real y seguro, con **al menos 32 bytes UTF-8**; la cadena mostrada es solo un marcador. Para MongoDB Atlas, verifica credenciales, permisos y que tu IP esté autorizada en el acceso de red del proyecto. No habilites acceso global sin autorización.

La aplicación carga primero `appSettings.yaml.token`, después `appSettings.yaml` y finalmente variables de entorno. Los valores locales y las variables de entorno prevalecen sobre la plantilla. No modifiques la plantilla versionada para insertar secretos.

### 3. Configurar el Email Worker

```powershell
Copy-Item .\src\b2b-e-commerce-email-worker\appSettings.yaml.token .\src\b2b-e-commerce-email-worker\appSettings.yaml
```

Ejemplo de estructura compatible con la configuración actual:

```yaml
Logging:
  LogLevel:
    Default: "Information"
    Microsoft.Hosting.Lifetime: "Information"

AWS:
  ServiceURL: "http://localhost:4567"
  Region: "us-east-1"
  QueueName: "cola-correos"

SMTP:
  Host: "smtp.gmail.com"
  Port: 587
  Security: "StartTls"
  SenderEmail: "CORREO_REMITENTE_AUTORIZADO"
  SenderName: "B2B E-Commerce"
  Username: "USUARIO_SMTP_AUTORIZADO"
  Password: "CREDENCIAL_SMTP_AUTORIZADA"
```

Si utilizas Gmail, emplea el mecanismo de autenticación SMTP permitido para esa cuenta (por ejemplo, una contraseña de aplicación cuando corresponda). No uses tu contraseña personal habitual. También puedes proporcionar credenciales mediante variables de entorno, por ejemplo `SMTP__Password`, evitando guardarlas en texto plano en el YAML.

> **Atención:** en el estado actual de `main`, el `docker-compose.yaml` **no incluye Mailpit**. El Worker está configurado mediante plantilla para Gmail SMTP. El README interno del Worker contiene referencias anteriores a Mailpit que no corresponden al Compose actual.

### 4. Seguridad de los archivos

`.gitignore` excluye `.env` y los archivos `**/appSettings.yaml`. Los archivos `appSettings.yaml.token` sí están versionados, por lo que **deben conservar únicamente marcadores y valores no sensibles**. Antes de hacer un commit:

```powershell
git status
git diff --cached
```

No publiques contraseñas, cadenas de conexión, tokens JWT ni tokens de LocalStack.

## Levantar LocalStack con Docker

**Docker Desktop debe estar iniciado**. Desde la raíz del repositorio:

```powershell
docker compose up -d
docker compose ps
docker compose logs localstack
```

La configuración actual levanta **un contenedor** llamado `b2b-localstack`:

- Puerto del host: **4567**.
- Puerto interno de LocalStack: **4566**.
- Servicio emulado: **SQS**.
- Script de inicialización montado desde `docker/localstack/init/`.

El mapeo `4567:4566` significa que la API y el Worker, al ejecutarse directamente en Windows, se conectan a `http://localhost:4567`. **No cambies sus URLs a `localhost:4566`** salvo que también cambies el mapeo del Compose.

Para detener los servicios:

```powershell
docker compose down
```

Para recrearlos después de cambiar el script de inicialización:

```powershell
docker compose down
docker compose up -d
docker compose logs localstack
```

> La configuración actual no define un volumen persistente para los datos de SQS. Al recrear el contenedor, no asumas que los mensajes o colas anteriores seguirán existiendo.

## Verificar las colas SQS

El script `docker/localstack/init/01-crear-colas.sh` debe crear:

| Cola | Función | Configuración |
| --- | --- | --- |
| `cola-correos` | Procesamiento normal de correos | `VisibilityTimeout=60` segundos |
| `cola-correos-dlq` | Mensajes fallidos | Política `maxReceiveCount=3` en la cola principal |

**No necesitas instalar AWS CLI en Windows** para verificar las colas: LocalStack incluye `awslocal` dentro del contenedor.

```powershell
docker exec b2b-localstack awslocal sqs list-queues
```

Deberían aparecer ambas colas. Para consultar los atributos de la principal:

```powershell
docker exec b2b-localstack awslocal sqs get-queue-attributes --queue-url http://sqs.us-east-1.localhost.localstack.cloud:4566/000000000000/cola-correos --attribute-names All
```

### Problema frecuente en Windows: saltos de línea CRLF

**Síntoma:** LocalStack inicia, pero no aparecen las colas; en los logs puede aparecer un error de Bash similar a `bad interpreter`, `$'\r': command not found` o `syntax error`.

**Causa habitual:** el archivo `.sh` fue guardado con finales de línea de Windows (**CRLF**) en vez de Unix (**LF**).

**Solución con VS Code:**

1. Abre `docker/localstack/init/01-crear-colas.sh`.
2. En la barra inferior derecha, haz clic en **CRLF**.
3. Selecciona **LF** y guarda el archivo.
4. Recrea LocalStack:

```powershell
docker compose down
docker compose up -d
docker compose logs localstack
docker exec b2b-localstack awslocal sqs list-queues
```

**Alternativa con Git (si Git está configurado para conversiones de saltos de línea):** establece una regla explícita en `.gitattributes` para evitar que el problema vuelva a ocurrir:

```gitattributes
*.sh text eol=lf
```

Esa regla es una **mejora recomendada**; no forma parte del repositorio en el momento de redactar este README.

Si las colas aún no se crean, revisa los logs de inicialización y verifica que el token de LocalStack sea válido. También puedes ejecutar manualmente el script **dentro del contenedor**, después de corregir los finales de línea:

```powershell
docker exec b2b-localstack bash /etc/localstack/init/ready.d/01-crear-colas.sh
```

Si ese comando falla, inspecciona el mensaje concreto antes de intentar crear las colas manualmente.

## Ejecutar la API y el Email Worker

Abre **tres terminales**: una para Docker/logs (opcional), otra para la API y otra para el Worker. Los dos proyectos .NET se ejecutan **fuera de Docker** en el flujo local actual.

### 1. API

Desde la raíz:

```powershell
dotnet run --project .\src\b2b-e-commerce-api --launch-profile https
```

Según `launchSettings.json`, las direcciones de desarrollo son:

- HTTPS: `https://localhost:7053`
- HTTP: `http://localhost:5107`
- Swagger UI: `https://localhost:7053/swagger`
- OpenAPI JSON: `https://localhost:7053/openapi/v1.json`

Swagger/OpenAPI se habilita cuando el entorno ASP.NET Core es **Development**. Si aparece un error de certificado local HTTPS:

```powershell
dotnet dev-certs https --trust
```

Confirma los puertos reales en la salida de `dotnet run`, porque podrían variar si modificas el perfil de lanzamiento.

### 2. Email Worker

En otra terminal, desde la raíz:

```powershell
dotnet run --project .\src\b2b-e-commerce-email-worker
```

El Worker busca `cola-correos`, procesa los mensajes, envía el correo mediante SMTP y elimina de SQS los mensajes procesados correctamente. Si falla el procesamiento, SQS puede reintentarlo y eventualmente trasladarlo a la DLQ.

**Orden recomendado:** 1) Docker/LocalStack; 2) verificar colas; 3) API; 4) Email Worker.

## Probar el flujo de registro

Con todos los servicios activos:

1. Abre Swagger UI en `https://localhost:7053/swagger`.
2. Ejecuta `POST /v1/autenticacion/registrar` con un correo de prueba al que tengas acceso y datos válidos.
3. Comprueba que la API cree el registro pendiente en MongoDB y publique el mensaje en `cola-correos`.
4. Observa los logs del Worker y verifica la recepción del correo en la bandeja correspondiente.
5. Ejecuta `POST /v1/autenticacion/verificar-correo` con el código recibido.
6. Si el código expiró y el registro pendiente continúa vigente, prueba `POST /v1/autenticacion/reenviar-codigo`.

**Comportamiento esperado:** el código de verificación vence en 10 minutos, el registro pendiente vence en 24 horas y se permiten hasta 5 intentos fallidos por código. El reenvío genera otro código y reinicia el contador de intentos.

> Los correos se envían a través del SMTP configurado: evita usar direcciones de terceros sin autorización durante las pruebas.

## Solución de problemas

| Problema | Posible causa y solución |
| --- | --- |
| `docker compose` solicita `LOCALSTACK_AUTH_TOKEN` | Falta `.env` en la raíz o la variable está vacía. Copia `env.example` y configura tu token. |
| Docker no responde / no encuentra el daemon | Inicia Docker Desktop y espera a que el motor esté listo. Prueba `docker info`. |
| LocalStack arranca, pero las colas no existen | Revisa `docker compose logs localstack`; verifica que `01-crear-colas.sh` use **LF**, no CRLF, y recrea el contenedor. |
| `QueueDoesNotExistException` en la API o el Worker | Verifica ambas colas con `awslocal sqs list-queues`, el nombre `cola-correos` y el endpoint `http://localhost:4567`. |
| Error de conexión a SQS | Confirma `docker compose ps`, puerto `4567:4566` y que Docker no esté detenido. |
| `Jwt:Secreto debe contener...` | Completa el secreto JWT local con al menos 32 bytes UTF-8; no dejes marcadores `__...__`. |
| `Mongo:ConnectionString no está configurada` | Crea el YAML local de la API y configura la cadena de conexión. |
| Error de autenticación o timeout de MongoDB | Revisa credenciales, acceso de red/IP de Atlas, nombre de base de datos y conectividad. |
| El Worker inicia, pero no envía correos | Revisa logs del Worker, valores `SMTP`, credenciales autorizadas y conectividad al servidor SMTP. |
| Fallo de autenticación de Gmail | Comprueba que el método de autenticación esté habilitado y que la credencial SMTP sea válida. |
| No aparece Swagger | Ejecuta la API con perfil `https` y entorno `Development`; visita `/swagger`. |
| HTTPS da error de certificado | Ejecuta `dotnet dev-certs https --trust` en tu máquina. |
| Puerto `4567` ocupado | Identifica el proceso o contenedor que lo usa; no cambies el mapeo sin actualizar las URLs de la API y el Worker. |
| Mensajes en `cola-correos-dlq` | Revisa el error del Worker y el formato de los mensajes; corrige la causa antes de reintentar. |

### Comandos útiles de diagnóstico

```powershell
# Contenedores activos
docker compose ps

# Logs de LocalStack
docker compose logs --tail=100 localstack

# Ver colas
docker exec b2b-localstack awslocal sqs list-queues

# Comprobar versión de .NET
dotnet --version

# Ver cambios antes de subirlos
git status
```

## Guía de contribución

### Flujo recomendado con Git

1. Actualiza tu rama principal: `git switch main` y `git pull origin main`.
2. Crea una rama descriptiva, por ejemplo: `git switch -c nombre/descripcion-cambio`.
3. Implementa los cambios respetando la separación entre API, BLL, repositorios, modelos y utilidades.
4. Ejecuta al menos `dotnet build .\b2b-e-commerce-backend.sln` y las pruebas disponibles.
5. Comprueba `git status` y `git diff` para evitar subir archivos locales o credenciales.
6. Publica tu rama y abre un **Pull Request** hacia `main` explicando el cambio y cómo se probó.

### Convenciones y buenas prácticas

- Evita incluir lógica de negocio directamente en los controladores.
- Mantén la configuración sensible fuera del repositorio.
- No expongas hashes, contraseñas, cadenas de conexión ni detalles internos en las respuestas HTTP.
- Si agregas variables de configuración, documenta sus nombres y valores esperados **sin publicar secretos**.
- Si modificas SQS, revisa tanto el productor (API) como el consumidor (Worker) y el script de creación de colas.
- Conserva finales de línea **LF** en los scripts Bash.
- Actualiza este README cuando cambien los pasos de instalación, los puertos o las dependencias.

## Estado y alcance de esta documentación

Este README describe el estado de la rama `main` revisado el **9 de noviembre de 2026**. No presupone que la API o el Worker estén dockerizados ni que exista un pipeline CI/CD ya desplegado. La infraestructura actual de Docker Compose contiene **LocalStack**, mientras que MongoDB y el servidor SMTP son servicios externos configurables.

**Nota de mantenimiento:** el README dentro de `src/b2b-e-commerce-email-worker/` conserva referencias a Mailpit y a un flujo anterior. Para la puesta en marcha actual, usa como referencia principal este documento y los archivos reales `docker-compose.yaml` y `appSettings.yaml.token`.
