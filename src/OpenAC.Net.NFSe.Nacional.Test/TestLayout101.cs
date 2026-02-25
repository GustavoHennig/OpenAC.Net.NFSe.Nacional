using System;
using System.IO;
using OpenAC.Net.DFe.Core.Extensions;
using OpenAC.Net.NFSe.Nacional.Common;
using OpenAC.Net.NFSe.Nacional.Common.Model;
using OpenAC.Net.NFSe.Nacional.Common.Types;

namespace OpenAC.Net.NFSe.Nacional.Test;

[TestClass]
public class TestLayout101
{
    [TestMethod]
    public void ConfiguracaoPadrao_DeveUsarVersao101()
    {
        var openNFSeNacional = new OpenNFSeNacional();

        Assert.AreEqual(VersaoNFSe.Ve101, openNFSeNacional.Configuracoes.Geral.Versao);
        Assert.AreEqual(VersaoNFSe.Ve101, openNFSeNacional.Configuracoes.Arquivos.VersaoSchema);
    }

    [TestMethod]
    public void InfDps_DeveExporCamposMotivoEmissaoTI_Layout101()
    {
        var inf = new InfDps
        {
            MotivoEmissaoTomadorIntermediario = MotivoEmissaoTomadorIntermediario.RejeicaoNFSePrestador,
            ChaveNFSeRejeitada = "12345678901234567890123456789012345678901234567890"
        };

        Assert.AreEqual(MotivoEmissaoTomadorIntermediario.RejeicaoNFSePrestador, inf.MotivoEmissaoTomadorIntermediario);
        Assert.AreEqual(50, inf.ChaveNFSeRejeitada?.Length);
    }

    [TestMethod]
    public void TipoCst_DeveConterCodigosPrevistosNoSchema101()
    {
        Assert.AreEqual("49", TipoCST.OutrasOperacoesSaida.GetDFeValue());
        Assert.AreEqual("75", TipoCST.AquisicaoSubstituicaoTributaria.GetDFeValue());
        Assert.AreEqual("98", TipoCST.OutrasOperacoesEntrada.GetDFeValue());
        Assert.AreEqual("99", TipoCST.OutrasOperacoes.GetDFeValue());
    }

    [TestMethod]
    public void RtcTotais_Diferimento_DeveSerOpcional()
    {
        Assert.AreEqual(typeof(decimal?), typeof(RTCTotalIBSUF).GetProperty(nameof(RTCTotalIBSUF.ValorDiferimentoUF))?.PropertyType);
        Assert.AreEqual(typeof(decimal?), typeof(RTCTotalIBSMun).GetProperty(nameof(RTCTotalIBSMun.ValorDiferimentoMunicipal))?.PropertyType);
        Assert.AreEqual(typeof(decimal?), typeof(RTCTotalCBS).GetProperty(nameof(RTCTotalCBS.ValorDiferimentoCBS))?.PropertyType);
    }

    [TestMethod]
    public void InfNFSeId_DeveSerGeradoNoFormatoEsperado_Layout101()
    {
        var infNfSe = new InfNFSe
        {
            NumeroNFSe = 123,
            AmbienteGerador = AmbienteGerador.Nacional,
            DhProcessamento = new DateTimeOffset(2026, 2, 23, 10, 30, 0, TimeSpan.Zero),
            Emitente = new EmitenteNFSe
            {
                CNPJ = "12345678000195",
                Endereco = new EnderecoEmitente { CodMunicipio = "3550308" }
            }
        };

        infNfSe.Id = NFSeIdHelper.GerarId(infNfSe, 123456789);

        Assert.AreEqual(53, infNfSe.Id.Length);
        Assert.IsTrue(infNfSe.Id.StartsWith("NFS3550308"));
        Assert.AreEqual("2", infNfSe.Id.Substring(10, 1));
        Assert.AreEqual("12345678000195", infNfSe.Id.Substring(12, 14));
        Assert.AreEqual("0000000000123", infNfSe.Id.Substring(26, 13));
        Assert.AreEqual("2602", infNfSe.Id.Substring(39, 4));
        Assert.AreEqual("123456789", infNfSe.Id.Substring(43, 9));
        Assert.IsTrue(NFSeIdHelper.TryValidarId(infNfSe, out var erro), erro);
    }

    [TestMethod]
    public void InfNFSeId_DeveFalharQuandoTipoInscricaoNaoCorresponderAoEmitente()
    {
        var infNfSe = new InfNFSe
        {
            NumeroNFSe = 10,
            AmbienteGerador = AmbienteGerador.Prefeitura,
            DhProcessamento = new DateTimeOffset(2026, 1, 8, 11, 0, 0, TimeSpan.Zero),
            Emitente = new EmitenteNFSe
            {
                CNPJ = "12345678000195",
                Endereco = new EnderecoEmitente { CodMunicipio = "3550308" }
            }
        };

        var idValido = NFSeIdHelper.GerarId(infNfSe, 654321987);
        infNfSe.Id = "NFS" + idValido.Substring(3, 8) + "1" + idValido.Substring(12);

        Assert.IsFalse(NFSeIdHelper.TryValidarId(infNfSe, out var erro));
        Assert.IsTrue((erro ?? string.Empty).Contains("emitente da NFS-e"));
    }

    [TestMethod]
    public void GetSchema_DeveAjustarPastaQuandoPathSchemasApontaParaVersaoDiferente()
    {
        var config = new NFSeArquivoConfig
        {
            PathSchemas = Path.Combine("C:\\temp", "Schemas", "1.00"),
            VersaoSchema = VersaoNFSe.Ve101
        };

        var schema = config.GetSchema(SchemaNFSe.DPS);

        Assert.IsTrue(schema.EndsWith(Path.Combine("Schemas", "1.01", "DPS_v1.01.xsd")));
    }

    [TestMethod]
    public void GetSchema_DeveAjustarPastaQuandoPathSchemasApontaParaRaiz()
    {
        var config = new NFSeArquivoConfig
        {
            PathSchemas = Path.Combine("C:\\temp", "Schemas"),
            VersaoSchema = VersaoNFSe.Ve101
        };

        var schema = config.GetSchema(SchemaNFSe.Evento);

        Assert.IsTrue(schema.EndsWith(Path.Combine("Schemas", "1.01", "pedRegEvento_v1.01.xsd")));
    }

    [TestMethod]
    public void DpsId_DeveSerGeradoComTamanhoFixoConformeSchema()
    {
        var dps = new Dps
        {
            Informacoes = new InfDps
            {
                LocalidadeEmitente = "4314902",
                Serie = "1",
                NumeroDps = "1",
                Prestador = new PrestadorDps
                {
                    CNPJ = "13.344.998/0001-93"
                }
            }
        };

        try
        {
            dps.Assinar(new ConfiguracaoNFSe());
        }
        catch
        {
            // O objetivo do teste é validar a geração do Id antes da assinatura.
        }

        Assert.AreEqual(45, dps.Informacoes.Id.Length);
        Assert.AreEqual("DPS431490221334499800019300001000000000000001", dps.Informacoes.Id);
    }

    [TestMethod]
    public void DpsTotTrib_NaoOptanteNaoPodeInformarIndTotTribOuPTotTribSn()
    {
        var dpsComIndicador = CriarDpsParaValidacaoTotTrib(OptanteSimplesNacional.NaoOptante, indTotTrib: 0);

        var exIndicador = AssertThrows<InvalidOperationException>(() => dpsComIndicador.Assinar(new ConfiguracaoNFSe()));
        StringAssert.Contains(exIndicador.Message, "indTotTrib");

        var dpsComPercentualSn = CriarDpsParaValidacaoTotTrib(OptanteSimplesNacional.NaoOptante, pTotTribSn: 7.25m);

        var exPercentual = AssertThrows<InvalidOperationException>(() => dpsComPercentualSn.Assinar(new ConfiguracaoNFSe()));
        StringAssert.Contains(exPercentual.Message, "pTotTribSN");
    }

    [TestMethod]
    public void DpsTotTrib_MeiNaoPodeInformarPTotTribSn()
    {
        var dps = CriarDpsParaValidacaoTotTrib(OptanteSimplesNacional.OptanteMEI, pTotTribSn: 3.10m);

        var ex = AssertThrows<InvalidOperationException>(() => dps.Assinar(new ConfiguracaoNFSe()));
        StringAssert.Contains(ex.Message, "MEI");
        StringAssert.Contains(ex.Message, "pTotTribSN");
    }

    [TestMethod]
    public void TotalTributos_CompatibilidadePercetualSimples_DeveMapearParaNovoCampo()
    {
        var total = new TotalTributos();

#pragma warning disable CS0618
        total.PercetualSimples = 4.20m;
        var valorLegado = total.PercetualSimples;
#pragma warning restore CS0618

        Assert.AreEqual(4.20m, total.PercentualTotalSimplesNacional);
        Assert.AreEqual(4.20m, valorLegado);
    }

    private static Dps CriarDpsParaValidacaoTotTrib(OptanteSimplesNacional optante, int? indTotTrib = null, decimal? pTotTribSn = null)
    {
        return new Dps
        {
            Informacoes = new InfDps
            {
                LocalidadeEmitente = "4314902",
                Serie = "1",
                NumeroDps = "1",
                Prestador = new PrestadorDps
                {
                    CNPJ = "13.344.998/0001-93",
                    Regime = new RegimeTributario
                    {
                        OptanteSimplesNacional = optante
                    }
                },
                Valores = new ValoresDps
                {
                    Tributos = new TributosNFSe
                    {
                        Total = new TotalTributos
                        {
                            IndicadorTotal = indTotTrib,
                            PercentualTotalSimplesNacional = pTotTribSn
                        }
                    }
                }
            }
        };
    }

    private static TException AssertThrows<TException>(Action action) where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException ex)
        {
            return ex;
        }
        catch (Exception ex)
        {
            Assert.Fail($"Expected exception {typeof(TException).Name}, but got {ex.GetType().Name}: {ex.Message}");
        }

        Assert.Fail($"Expected exception {typeof(TException).Name}, but no exception was thrown.");
        return null!;
    }
}
