// ***********************************************************************
// Assembly         : OpenAC.Net.NFSe.Nacional
// Author           : RFTD
// Created          : 09-09-2023
//
// Last Modified By : RFTD
// Last Modified On : 09-09-2023
// ***********************************************************************
// <copyright file="Dps.cs" company="OpenAC .Net">
//		        		   The MIT License (MIT)
//	     		    Copyright (c) 2014-2023 Grupo OpenAC.Net
//
//	 Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the "Software"),
// to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following conditions:
//	 The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//	 THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
// IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
// DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE,
// ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.
// </copyright>
// <summary></summary>
// ***********************************************************************

using OpenAC.Net.Core.Extensions;
using OpenAC.Net.DFe.Core.Attributes;
using OpenAC.Net.DFe.Core.Common;
using OpenAC.Net.DFe.Core.Document;
using OpenAC.Net.DFe.Core.Serializer;
using OpenAC.Net.NFSe.Nacional.Common.Types;
using System;
using System.Text;

namespace OpenAC.Net.NFSe.Nacional.Common.Model;

/// <summary>
/// Representa o documento DPS (Documento de Prestação de Serviço) para NFSe Nacional.
/// </summary>
[DFeSignInfoElement("infDPS")]
[DFeRoot("DPS", Namespace = "http://www.sped.fazenda.gov.br/nfse")]
public sealed class Dps : DFeSignDocument<Dps>
{
    #region Constructors

    /// <summary>
    /// Inicializa uma nova instância da classe <see cref="Dps"/>.
    /// </summary>
    public Dps()
    {
        Signature = new DFeSignature();
    }

    #endregion Constructors

    #region Properties

    /// <summary>
    /// Obtém ou define a versão do layout do DPS.
    /// </summary>
    [DFeAttribute(TipoCampo.Enum, "versao", Ocorrencia = Ocorrencia.Obrigatoria)]
    public VersaoNFSe Versao { get; set; } = VersaoNFSe.Ve101;

    /// <summary>
    /// Obtém ou define as informações do DPS.
    /// </summary>
    [DFeElement("infDPS", Ocorrencia = Ocorrencia.Obrigatoria)]
    public InfDps Informacoes { get; set; } = new();

    #endregion Properties

    #region Methods

    /// <summary>
    /// Assina o documento DPS utilizando as configurações fornecidas.
    /// </summary>
    /// <param name="configuracao">Configuração da NFSe.</param>
    public void Assinar(ConfiguracaoNFSe configuracao)
    {
        if (Informacoes.Id.IsEmpty())
        {
            Informacoes.Id = GerarIdDps(Informacoes);
        }

        ValidarTotTribPorRegimeSimples(Informacoes);

        var options = DFeSaveOptions.DisableFormatting;
        if (configuracao.Geral.RetirarAcentos)
            options |= DFeSaveOptions.RemoveAccents;

        AssinarDocumento(configuracao.Certificados.ObterCertificadoAssinatura(), options, false);
    }

    private static string GerarIdDps(InfDps infDps)
    {
        var codigoMunicipio = FormatarCampoNumerico(infDps.LocalidadeEmitente, 7, nameof(infDps.LocalidadeEmitente));
        var serie = FormatarCampoNumerico(infDps.Serie, 5, nameof(infDps.Serie));
        var numeroDps = FormatarCampoNumerico(infDps.NumeroDps, 15, nameof(infDps.NumeroDps));

        string tipoInscricao;
        string inscricao;

        var cnpj = SomenteDigitos(infDps.Prestador.CNPJ);
        if (!cnpj.IsEmpty())
        {
            if (cnpj.Length > 14)
                throw new InvalidOperationException("CNPJ do prestador deve conter no máximo 14 dígitos.");

            tipoInscricao = "2";
            inscricao = cnpj.PadLeft(14, '0');
        }
        else
        {
            var cpf = SomenteDigitos(infDps.Prestador.CPF);
            if (cpf.IsEmpty())
                throw new InvalidOperationException("Prestador deve informar CPF ou CNPJ para geração do Id da DPS.");

            if (cpf.Length > 11)
                throw new InvalidOperationException("CPF do prestador deve conter no máximo 11 dígitos.");

            tipoInscricao = "1";
            inscricao = cpf.PadLeft(14, '0');
        }

        return $"DPS{codigoMunicipio}{tipoInscricao}{inscricao}{serie}{numeroDps}";
    }

    private static string FormatarCampoNumerico(string? valor, int tamanho, string nomeCampo)
    {
        var digitos = SomenteDigitos(valor);
        if (digitos.Length > tamanho)
            throw new InvalidOperationException($"{nomeCampo} deve conter no máximo {tamanho} dígitos.");

        return digitos.PadLeft(tamanho, '0');
    }

    private static string SomenteDigitos(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
            return string.Empty;

        var texto = valor ?? string.Empty;
        var sb = new StringBuilder(texto.Length);
        foreach (var c in texto)
        {
            if (char.IsDigit(c))
                sb.Append(c);
        }

        return sb.ToString();
    }

    private static void ValidarTotTribPorRegimeSimples(InfDps infDps)
    {
        var total = infDps.Valores.Tributos.Total;
        var indTotTribInformado = total.IndicadorTotal.HasValue;
        var pTotTribSnInformado = total.PercentualTotalSimplesNacional.HasValue;

        var optanteSimples = infDps.Prestador.Regime.OptanteSimplesNacional;
        switch (optanteSimples)
        {
            case OptanteSimplesNacional.NaoOptante:
                if (indTotTribInformado || pTotTribSnInformado)
                {
                    throw new InvalidOperationException(
                        "Para emitente Não Optante do Simples Nacional, os campos indTotTrib e pTotTribSN não podem ser informados.");
                }

                break;

            case OptanteSimplesNacional.OptanteMEI:
                if (pTotTribSnInformado)
                {
                    throw new InvalidOperationException(
                        "Para emitente MEI, o campo pTotTribSN não pode ser informado.");
                }

                break;
        }
    }

    #endregion Methods
}
