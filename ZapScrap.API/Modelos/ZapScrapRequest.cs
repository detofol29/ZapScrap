using ZapScrap.API.Enuns;

namespace ZapScrap.API.Modelos
{
    public class ZapScrapRequest
    {
        public string Estado { get; set; }
        public string Cidade { get; set; }
        public string? Bairro { get; set; }
        public TipoDeImovel? TipoImovel { get; set; }
        public int? ValorMinimo { get; set; }
        public int? ValorMaximo { get; set; }
        public TipoDeAnuncio? TipoAnuncio { get; set; }
        public int? QuantidadeMinimaDeBanheiros { get; set; }
        public int? QuantidadeMinimaDeQuartos { get; set; }
        public int? QuantidadeMinimaDeVagasNaGaragem { get; set; }
        public int? AreaMinima { get; set; }
        public int Paginas { get; set; } = 1;
    }
}
