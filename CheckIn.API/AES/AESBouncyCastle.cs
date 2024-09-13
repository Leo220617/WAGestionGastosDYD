using System;

using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;

namespace GreenPayApi.AES
{
    public class AESBouncyCastle{
        
        /**
            Nombre: CipherAESCRTWithBouncyCastle
            Descripción: Cifra con AES en modo CTR utilizando la librería Bouncy Castle.
            Retorna: Un arreglo de bytes que representa los datos cifrados con AES en modo CTR.
            Parámetros: - keyBytes: Arreglo de 16 bytes con la llave para cifrar en AES.
                        - counter: Arreglo de 16 bytes con el contador para el modo CTR de AES.
                        - inputBytes: Arreglo de bytes que contiene los datos a cifrar.
        */
        public static byte[] CipherAESCRTWithBouncyCastle(byte[] keyBytes, byte[] counter, byte[] inputBytes){
            
            IBufferedCipher cipher = CipherUtilities.GetCipher("AES/CTR/NoPadding");
            cipher.Init(true, new ParametersWithIV(ParameterUtilities.CreateKeyParameter("AES", keyBytes), counter));
            byte[] encryptedBytes = cipher.DoFinal(inputBytes);

            return encryptedBytes;
        }

    }
}