using System;
using System.Text;
using System.Threading.Tasks;

namespace Ropu.Shared
{
    public class PacketEncryption
    {
        readonly KeysClient _keysClient;

        public PacketEncryption(KeysClient keysClient)
        {
            _keysClient = keysClient;
        }

        (bool isUser, byte keyId) GetKeyInfo(byte[] packetBuffer, int length)
        {
            var packet = packetBuffer.AsSpan(0, length);
            bool isUser = (packet[0] & 0x80) != 0;
            byte keyId = (byte)((packet[0] >> 5) & 0x03);
            return (isUser, keyId);
        }

        uint GetSourceId(byte[] packet)
        {
            ReadOnlySpan<byte> sourceIdData = packet.AsSpan(1);
            return BitConverter.ToUInt32(sourceIdData);
        }

        public async ValueTask<int> Decrypt(byte[] packetBuffer, int length, byte[] plainText)
        {
            (bool isUser, byte keyId) = GetKeyInfo(packetBuffer, length);

            uint sourceId = GetSourceId(packetBuffer);

            var encryption = await _keysClient.GetKey(isUser, sourceId, keyId);
            if(encryption == null)
            {
                Console.Error.WriteLine($"Failed to find a key for packet, isUser {isUser}, keyId {keyId}");
                return 0;
            }
            return Decrypt(packetBuffer, length, encryption.Encryption, plainText);
        }

        int Decrypt(byte[] packetBuffer, int length, AesGcmEncryption encryption, byte[] plainText)
        {
            var packet = packetBuffer.AsSpan(0, length);
            if(packet.Length < 22)
            {
                throw new Exception($"Packet could no be decrypted because it is to small length {packet.Length}");
            }
            uint sourceId = BitConverter.ToUInt32(packet.Slice(1));
            int packetCounter = BitConverter.ToInt32(packet.Slice(5));
            var tag = packet.Slice(9, 12);
            var cipherText = packet.Slice(21);

            encryption.Decrypt(cipherText, packetCounter, plainText.AsSpan(0, cipherText.Length), tag);

            return cipherText.Length;
        }

        public int CreateEncryptedPacket(Span<byte> payload, Span<byte> packet, bool isGroup, 
            uint source, CachedEncryptionKey keyInfo)
        {
            packet[0] = (byte)(
                (isGroup ? 0x00 : 0x80) | // Type
                (keyInfo.KeyId << 5));    // KeyId

            // SourceId
            BitConverter.TryWriteBytes(packet.Slice(1), source);

            // packet counter
            var packetCounter = keyInfo.GetPacketCounter();
            BitConverter.TryWriteBytes(packet.Slice(5), packetCounter);

            Span<byte> tag = packet.Slice(9, 12);

            keyInfo.Encryption.Encrypt(payload, packet.Slice(9 + 12, payload.Length), tag, (int)packetCounter);

            return 9 + 12 + payload.Length;
        }

        string ToString(Span<byte> data)
        {
            StringBuilder builder = new StringBuilder();
            foreach(var val in data)
            {
                builder.Append(val.ToString("X2"));
            }
            return builder.ToString();
        }
    }
}