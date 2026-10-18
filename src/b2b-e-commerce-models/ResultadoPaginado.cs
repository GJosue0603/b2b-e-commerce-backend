namespace b2b_e_commerce_models
{
    public class ResultadoPaginado<T>
    {
        public long CantidadTotalDeItems { get; set; }
        public IEnumerable<T> Items { get; set; }

        public int NumeroDePagina { get; set; }
        public int CantidadPorPagina { get; set; }
        public long TotalDePaginas { get; set; }

        public ResultadoPaginado(
            IEnumerable<T> items,
            long cantidadTotalDeItems,
            int numeroDePagina,
            int cantidadPorPagina
        )
        {
            Items = items ?? new List<T>();
            CantidadTotalDeItems = cantidadTotalDeItems;
            NumeroDePagina = numeroDePagina;
            CantidadPorPagina = cantidadPorPagina;
            TotalDePaginas = cantidadTotalDeItems / cantidadPorPagina + (cantidadTotalDeItems % cantidadPorPagina > 0 ? 1 : 0);
        }
    }
}