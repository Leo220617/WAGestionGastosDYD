using System;
using System.Security.Cryptography;
using System.IO;
using System.Collections.Generic;

namespace GreenPayApi.AES {

    public class AESCTRMode{

        /**
            Nombre: CipherAESCTR
            Descripción: Cifra con AES en modo CTR utilizando una implementación manual basada en clases nativas de .Net.
            Retorna: No retorna valores. La respuesta se escribe en el parámetro outputStream.
            Parámetros: - key: Arreglo de 16 bytes con la llave para cifrar en AES.
                        - counterBytes: Arreglo de 16 bytes con el contador para el modo CTR de AES.
                        - inputStream: Stream que contiene los datos a cifrar.
                        - outputStream: Stream con el contenido cifrado en AES en modo CTR.
        */
        public static void CipherAESCTR(byte[] key, byte[] counterBytes, Stream inputStream, Stream outputStream)
            {
                //  El modo CTR de AES se puede aplicar utilizando:
                //      - El modo de cifrado ECB de AES sin padding.
                //      - No usar vector de inicialización.
                //      - Utilizar un contador del mismo largo de la llave.
                SymmetricAlgorithm aes = new AesManaged { Mode = CipherMode.ECB, Padding = PaddingMode.None };

                // El tamaño del bloque de cifrado es de 16 bytes.
                int blockSize = aes.BlockSize / 8;

                // Tanto el largo de la llave como el del contador debe de ser del mismo tamaño (16 bytes)
                if (counterBytes.Length != blockSize)
                {
                    throw new ArgumentException(
                        string.Format(
                            "La llave y el contador deben de tener el mismo tamaño (actual: {0}, esperado: {1})",
                            counterBytes.Length, blockSize));
                }

                // Se copia el contador pues este valor va a mutar conforme avanza el cifrado.
                byte[] counter = (byte[])counterBytes.Clone();

                // Por medio de una cola, obtenemos los valores uno a uno para cifrar cada byte de un bloque.
                // Se guarda cada byte del counter que ha sido cifrado con la llave.
                Queue<byte> xorMask = new Queue<byte>();

                // Se crea un cifrador con la llave especificad y con un vextor de inicialización vacío.
                var zeroIv = new byte[blockSize];
                ICryptoTransform counterEncryptor = aes.CreateEncryptor(key, zeroIv);

                int b;
                while ((b = inputStream.ReadByte()) != -1)
                {
                    // Si no hay elementos del contador cifrados en la cola significa que estamos ante el primer byte de un
                    // bloque nuevo, por lo que se deben cifrar los 16 bytes del contador actual para cifrar el siguiente bloque.
                    if (xorMask.Count == 0)
                    {
                        var counterModeBlock = new byte[blockSize];

                        // Cifra con AES en modo ECB el arreglo del contador con el número actual y lo guarda en la variable counterModeBlock.
                        counterEncryptor.TransformBlock(counter, 0, counter.Length, counterModeBlock, 0);

                        // Se debe incrementar en 1 el valor del contador para que esté listo para usarse en el siguiente bloque.
                        for (var i2 = counter.Length - 1; i2 >= 0; i2--)
                        {
                            if (++counter[i2] != 0)
                            {
                                break;
                            }
                        }

                        // Se guardan en la cola todos los bytes cifrados del counter en orden para que sean aplicados al bloque por cifrar.
                        foreach (var b2 in counterModeBlock)
                        {
                            xorMask.Enqueue(b2);
                        }
                    }

                    // Se obtiene el siguiente valor de la cola.
                    var mask = xorMask.Dequeue();
                    // Se escribe valor en la cadena de respuesta.
                    // Esto implica realizar una operación XOR entre el byte del bloque a cifrar y el byte del contador del bloque actual cifrado
                    outputStream.WriteByte((byte)(((byte)b) ^ mask));
                }
            }

        }

}