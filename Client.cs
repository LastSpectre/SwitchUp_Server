using ProtoBuf;
using System.Net.Sockets;

namespace BA_Praxis_Library
{
    public class Client
    {
        // send request to server
        public ServerResponse RunRequest(string _ip, int _port, UserRequest _request)
        {
            // connect to server
            TcpClient tcpClient = new TcpClient();
            tcpClient.Connect(_ip, _port);

            // TODO: Hash password if time

            // get stream from client to send and receive data
            NetworkStream stream = tcpClient.GetStream();

            // Serialize data with protobuf | flush stream after usage
            Serializer.SerializeWithLengthPrefix(stream, _request, PrefixStyle.Fixed32);
            stream.Flush();

            // return the response of server
            return Serializer.DeserializeWithLengthPrefix<ServerResponse>(tcpClient.GetStream(), PrefixStyle.Fixed32);
        }
    }
}