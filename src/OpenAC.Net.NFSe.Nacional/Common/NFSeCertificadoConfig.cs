// ***********************************************************************
// Assembly         : OpenAC.Net.NFSe.Nacional
// Author           : RFTD
// Created          : 09-09-2023
//
// Last Modified By : RFTD
// Last Modified On : 09-09-2023
// ***********************************************************************
// <copyright file="NFSeCertificadoConfig.cs" company="OpenAC .Net">
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

using OpenAC.Net.DFe.Core.Common;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace OpenAC.Net.NFSe.Nacional.Common;

/// <summary>
/// Representa a configuração para certificados NFSe.
/// </summary>
public sealed class NFSeCertificadoConfig : DFeCertificadosConfigBase
{
    /// <summary>
    /// Obtém o certificado digital para uso em assinatura/transporte com flags
    /// adequadas para cenários de serviço (evita erro "Keyset does not exist").
    /// </summary>
    public X509Certificate2 ObterCertificadoAssinatura()
    {
        if (CertificadoBytes is { Length: > 0 })
        {
            return CriarCertificadoComFallback(flags => new X509Certificate2(CertificadoBytes, Senha, flags));
        }

        if (!string.IsNullOrWhiteSpace(Certificado) && File.Exists(Certificado))
        {
            return CriarCertificadoComFallback(flags => new X509Certificate2(Certificado, Senha, flags));
        }

        var certificadoPadrao = ObterCertificado();
        ValidarChavePrivada(certificadoPadrao);
        return certificadoPadrao;
    }

    private static X509Certificate2 CriarCertificadoComFallback(Func<X509KeyStorageFlags, X509Certificate2> factory)
    {
        var flagsCandidates = new[]
        {
            X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet,
            X509KeyStorageFlags.UserKeySet | X509KeyStorageFlags.PersistKeySet
        };

        Exception? lastException = null;

        foreach (var flags in flagsCandidates)
        {
            try
            {
                var certificado = factory(flags);
                ValidarChavePrivada(certificado);
                return certificado;
            }
            catch (Exception ex) when (ex is CryptographicException || ex is InvalidOperationException)
            {
                lastException = ex;
            }
        }

        throw new CryptographicException(
            "Não foi possível carregar a chave privada do certificado com os key stores disponíveis.",
            lastException);
    }

    private static void ValidarChavePrivada(X509Certificate2 certificado)
    {
        if (!certificado.HasPrivateKey)
            throw new CryptographicException("Certificado digital sem chave privada.");

        using var rsa = certificado.GetRSAPrivateKey();
        if (rsa == null)
            throw new CryptographicException("Não foi possível acessar a chave privada RSA do certificado digital.");
    }
}