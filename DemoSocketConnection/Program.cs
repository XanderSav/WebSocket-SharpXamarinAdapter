using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebSocketSharpXamarinAdapter.DTO;
using WebSocketSharpXamarinAdapter.WebSocket;

namespace DemoSocketConnection
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var socketParameters = new SocketParameters(
                    "<server_returns_stomp_user_after_auth_by_user_credentials>",
                    "<server_returns_stomp_password_after_auth_by_user_credentials>",
                    "<server_returns_sessionID_after_auth_by_user_credentials>",
                    "<umID_set_in_auth_response_cookies>",
                    "<paste_host_address_here>",
                    "<paste_host_domain_here>",
                    "<server_id_to_connect_optional>");

            var socket = new WebSocketImplementation();

            socket.Init(socketParameters);
            var connectionSuccess = await socket.Open();
            
            if (connectionSuccess)
            {
                Console.WriteLine($"Connected to {socketParameters.Host} successfully");
            }
            else
            {
                Console.WriteLine($"Something went wrong while trying to connect {socketParameters.Host}");
            }

            Console.ReadLine();
        }        
    }
}
