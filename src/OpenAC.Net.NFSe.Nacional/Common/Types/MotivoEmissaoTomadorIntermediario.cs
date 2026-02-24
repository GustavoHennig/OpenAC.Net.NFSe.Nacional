using OpenAC.Net.DFe.Core.Attributes;

namespace OpenAC.Net.NFSe.Nacional.Common.Types;

/// <summary>
/// Motivo da Emissão da DPS pelo Tomador/Intermediário.
/// </summary>
public enum MotivoEmissaoTomadorIntermediario
{
    /// <summary>
    /// 1 - Importação de Serviço.
    /// </summary>
    [DFeEnum("1")]
    ImportacaoServico = 1,

    /// <summary>
    /// 2 - Obrigado por legislação municipal.
    /// </summary>
    [DFeEnum("2")]
    ObrigadoPorLegislacaoMunicipal = 2,

    /// <summary>
    /// 3 - Recusa de emissão pelo prestador.
    /// </summary>
    [DFeEnum("3")]
    RecusaEmissaoPrestador = 3,

    /// <summary>
    /// 4 - Rejeição da NFS-e emitida pelo prestador.
    /// </summary>
    [DFeEnum("4")]
    RejeicaoNFSePrestador = 4
}
