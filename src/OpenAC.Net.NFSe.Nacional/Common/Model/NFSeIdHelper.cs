using System;
using System.Globalization;
using System.Security.Cryptography;
using OpenAC.Net.NFSe.Nacional.Common.Types;

namespace OpenAC.Net.NFSe.Nacional.Common.Model;

/// <summary>
/// Utilitário para geração e validação do identificador <c>NFSe/infNFSe/@Id</c>.
/// Regra (53 posições): NFS + cMun(7) + ambGer(1) + tpInscFed(1) + inscFed(14) + nNFSe(13) + aaMM(4) + cNum(9) + DV(1).
/// </summary>
public static class NFSeIdHelper
{
    /// <summary>
    /// Gera o identificador da NFS-e com código numérico aleatório de 9 posições.
    /// </summary>
    /// <param name="infNFSe">Informações da NFS-e.</param>
    /// <returns>Identificador no formato da versão 1.01.</returns>
    public static string GerarId(InfNFSe infNFSe)
    {
        if (infNFSe == null) throw new ArgumentNullException(nameof(infNFSe));

        return GerarId(infNFSe, GerarCodigoNumerico());
    }

    /// <summary>
    /// Gera o identificador da NFS-e com código numérico informado.
    /// </summary>
    /// <param name="infNFSe">Informações da NFS-e.</param>
    /// <param name="codigoNumerico">Código numérico de 9 posições.</param>
    /// <returns>Identificador no formato da versão 1.01.</returns>
    public static string GerarId(InfNFSe infNFSe, int codigoNumerico)
    {
        if (infNFSe == null) throw new ArgumentNullException(nameof(infNFSe));
        if (codigoNumerico < 0 || codigoNumerico > 999999999)
            throw new ArgumentOutOfRangeException(nameof(codigoNumerico), "O código numérico deve estar entre 0 e 999999999.");

        var municipioEmitente = ExtrairCodigoMunicipioEmitente(infNFSe);
        var ambienteGerador = ObterAmbienteGerador(infNFSe);
        var (tipoInscricao, inscricaoFederal) = ExtrairInscricaoFederalEmitente(infNFSe);
        var numeroNfSe = ObterNumeroNfSe(infNFSe);
        var anoMesEmissao = ObterAnoMesEmissao(infNFSe);
        var codigoNumericoTexto = codigoNumerico.ToString("D9", CultureInfo.InvariantCulture);

        var baseSemDv = municipioEmitente + ambienteGerador + tipoInscricao + inscricaoFederal + numeroNfSe + anoMesEmissao + codigoNumericoTexto;
        var dv = CalcularDigitoVerificador(baseSemDv);

        return $"NFS{baseSemDv}{dv}";
    }

    /// <summary>
    /// Valida se o identificador da NFS-e é consistente com os campos do documento.
    /// </summary>
    /// <param name="infNFSe">Informações da NFS-e.</param>
    /// <param name="erro">Mensagem detalhada quando inválido.</param>
    /// <returns><c>true</c> quando válido, caso contrário <c>false</c>.</returns>
    public static bool TryValidarId(InfNFSe infNFSe, out string? erro)
    {
        if (infNFSe == null)
        {
            erro = "As informações da NFS-e não foram informadas.";
            return false;
        }

        var id = infNFSe.Id?.Trim() ?? string.Empty;
        if (id.Length != 53)
        {
            erro = "O identificador da NFS-e deve possuir 53 posições.";
            return false;
        }

        if (!id.StartsWith("NFS", StringComparison.Ordinal))
        {
            erro = "O identificador da NFS-e deve iniciar com o literal 'NFS'.";
            return false;
        }

        var corpo = id.Substring(3);
        if (!EhSomenteDigitos(corpo))
        {
            erro = "O identificador da NFS-e deve conter apenas dígitos após o literal 'NFS'.";
            return false;
        }

        var cMunId = id.Substring(3, 7);
        var ambGerId = id.Substring(10, 1);
        var tipoInscId = id.Substring(11, 1);
        var inscFedId = id.Substring(12, 14);
        var numeroNfSeId = id.Substring(26, 13);
        var anoMesId = id.Substring(39, 4);
        var baseSemDv = id.Substring(3, 49);
        var dvId = id.Substring(52, 1);

        try
        {
            var municipioEmitente = ExtrairCodigoMunicipioEmitente(infNFSe);
            if (!string.Equals(cMunId, municipioEmitente, StringComparison.Ordinal))
            {
                erro = "Cód.Mun do identificador diverge do código do município do endereço do emitente da NFS-e.";
                return false;
            }

            var ambGerador = ObterAmbienteGerador(infNFSe);
            if (!string.Equals(ambGerId, ambGerador, StringComparison.Ordinal))
            {
                erro = "Ambiente gerador do identificador diverge do campo ambGer da NFS-e.";
                return false;
            }

            var (tipoInscricao, inscricaoFederal) = ExtrairInscricaoFederalEmitente(infNFSe);
            if (!string.Equals(tipoInscId, tipoInscricao, StringComparison.Ordinal))
            {
                erro = "Tipo de inscrição federal do identificador diverge dos dados do emitente da NFS-e.";
                return false;
            }

            if (!string.Equals(inscFedId, inscricaoFederal, StringComparison.Ordinal))
            {
                erro = "Inscrição federal do identificador diverge dos dados do emitente da NFS-e.";
                return false;
            }

            var numeroNfSe = ObterNumeroNfSe(infNFSe);
            if (!string.Equals(numeroNfSeId, numeroNfSe, StringComparison.Ordinal))
            {
                erro = "nNFSe do identificador diverge do campo nNFSe da NFS-e.";
                return false;
            }

            var anoMesEmissao = ObterAnoMesEmissao(infNFSe);
            if (!string.Equals(anoMesId, anoMesEmissao, StringComparison.Ordinal))
            {
                erro = "AnoMes de emissão do identificador diverge da data de emissão/processamento da NFS-e.";
                return false;
            }
        }
        catch (InvalidOperationException ex)
        {
            erro = ex.Message;
            return false;
        }

        var dvCalculado = CalcularDigitoVerificador(baseSemDv).ToString(CultureInfo.InvariantCulture);
        if (!string.Equals(dvId, dvCalculado, StringComparison.Ordinal))
        {
            erro = "DV do identificador da NFS-e é inválido.";
            return false;
        }

        erro = null;
        return true;
    }

    /// <summary>
    /// Valida o identificador da NFS-e e lança exceção em caso de inconsistência.
    /// </summary>
    /// <param name="infNFSe">Informações da NFS-e.</param>
    public static void ValidarIdOuLancar(InfNFSe infNFSe)
    {
        if (TryValidarId(infNFSe, out var erro)) return;

        throw new InvalidOperationException($"Identificador da NFS-e inválido: {erro}");
    }

    private static string ExtrairCodigoMunicipioEmitente(InfNFSe infNFSe)
    {
        var cMun = ApenasDigitos(infNFSe.Emitente?.Endereco?.CodMunicipio);
        if (cMun.Length != 7)
            throw new InvalidOperationException("Código do município do endereço do emitente deve possuir 7 dígitos.");

        return cMun;
    }

    private static (string TipoInscricao, string InscricaoFederal) ExtrairInscricaoFederalEmitente(InfNFSe infNFSe)
    {
        var cnpj = ApenasDigitos(infNFSe.Emitente?.CNPJ);
        var cpf = ApenasDigitos(infNFSe.Emitente?.CPF);

        var temCnpj = cnpj.Length > 0;
        var temCpf = cpf.Length > 0;

        if (temCnpj == temCpf)
            throw new InvalidOperationException("Emitente deve informar somente CPF ou somente CNPJ para formação do identificador da NFS-e.");

        if (temCpf)
        {
            if (cpf.Length != 11)
                throw new InvalidOperationException("CPF do emitente deve possuir 11 dígitos.");

            return ("1", cpf.PadLeft(14, '0'));
        }

        if (cnpj.Length != 14)
            throw new InvalidOperationException("CNPJ do emitente deve possuir 14 dígitos.");

        return ("2", cnpj);
    }

    private static string ObterNumeroNfSe(InfNFSe infNFSe)
    {
        if (infNFSe.NumeroNFSe < 1)
            throw new InvalidOperationException("nNFSe deve ser maior que zero para formação do identificador da NFS-e.");

        return infNFSe.NumeroNFSe.ToString("D13", CultureInfo.InvariantCulture);
    }

    private static string ObterAmbienteGerador(InfNFSe infNFSe)
    {
        return infNFSe.AmbienteGerador switch
        {
            AmbienteGerador.Prefeitura => "1",
            AmbienteGerador.Nacional => "2",
            _ => throw new InvalidOperationException("Valor de ambGer inválido para formação do identificador da NFS-e.")
        };
    }

    private static string ObterAnoMesEmissao(InfNFSe infNFSe)
    {
        var dataReferencia = infNFSe.DhProcessamento;
        if (dataReferencia == default && infNFSe.Dps?.Informacoes != null)
            dataReferencia = infNFSe.Dps.Informacoes.DhEmissao;

        if (dataReferencia == default)
            throw new InvalidOperationException("Data de emissão/processamento não informada para formação do identificador da NFS-e.");

        return dataReferencia.ToString("yyMM", CultureInfo.InvariantCulture);
    }

    private static int CalcularDigitoVerificador(string baseSemDv)
    {
        if (baseSemDv.Length != 49 || !EhSomenteDigitos(baseSemDv))
            throw new InvalidOperationException("Base para cálculo do DV deve possuir 49 dígitos.");

        // Regra inferida do padrão de chaves DF-e: módulo 11 com pesos cíclicos de 2 a 9.
        var soma = 0;
        var peso = 2;

        for (var i = baseSemDv.Length - 1; i >= 0; i--)
        {
            soma += (baseSemDv[i] - '0') * peso;
            peso = peso == 9 ? 2 : peso + 1;
        }

        var resto = soma % 11;
        var dv = 11 - resto;
        if (dv >= 10) dv = 0;

        return dv;
    }

    private static string ApenasDigitos(string? valor)
    {
        if (valor == null) return string.Empty;
        if (valor.Trim().Length == 0) return string.Empty;

        var buffer = new char[valor.Length];
        var pos = 0;
        for (var i = 0; i < valor.Length; i++)
        {
            if (!char.IsDigit(valor[i])) continue;

            buffer[pos] = valor[i];
            pos++;
        }

        return pos == 0 ? string.Empty : new string(buffer, 0, pos);
    }

    private static bool EhSomenteDigitos(string valor)
    {
        for (var i = 0; i < valor.Length; i++)
        {
            if (!char.IsDigit(valor[i])) return false;
        }

        return true;
    }

    private static int GerarCodigoNumerico()
    {
        var bytes = new byte[4];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(bytes);
        }

        return (int)(BitConverter.ToUInt32(bytes, 0) % 1000000000);
    }
}
