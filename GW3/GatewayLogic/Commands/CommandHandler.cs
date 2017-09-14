using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GatewayLogic.Commands
{
    abstract class CommandHandler
    {
        static readonly CommandHandler[] handlers = new CommandHandler[28];

        /// <summary>
        /// Setup the handles in the array for quick access.
        /// </summary>
        static CommandHandler()
        {
            handlers[0] = new Version();
            handlers[1] = new EventAdd();
            //handlers[2] = new EventCancel();
            //handlers[4] = new Write();
            handlers[6] = new Search();
            //handlers[11] = new ProtoError();
            //handlers[12] = new ClearChannel();
            //handlers[13] = new Beacon();
            handlers[15] = new ReadNotify();
            handlers[18] = new CreateChannel();
            //handlers[19] = new WriteNotify();
            //handlers[20] = new ClientName();
            //handlers[21] = new HostName();
            handlers[22] = new AccessRights();
            //handlers[23] = new Echo();
            //handlers[27] = new ChannelDisconnect();
        }

        /// <summary>
        /// Used to answer to a request
        /// </summary>
        /// <param name="command"></param>
        /// <param name="packet"></param>
        /// <param name="chain"></param>
        /// <param name="send"> </param>
        /// <returns></returns>
        public static void ExecuteRequestHandler(UInt16 command, GatewayConnection connection, DataPacket packet)
        {
            //Log.Write("Request "+command);
            if (!(command >= handlers.Length || handlers[command] == null))
                handlers[command].DoRequest(connection, packet);
        }

        /// <summary>
        /// Used to answer to a response
        /// </summary>
        /// <param name="command"></param>
        /// <param name="packet"></param>
        /// <param name="chain"></param>
        /// <param name="send"> </param>
        /// <returns></returns>
        public static void ExecuteResponseHandler(UInt16 command, GatewayConnection connection, DataPacket packet)
        {
            //Log.Write("Answer " + command);
            if (!(command >= handlers.Length || handlers[command] == null))
                handlers[command].DoResponse(connection, packet);
        }


        string name = null;
        /// <summary>
        /// Return a cached (after the first call) of the class name.
        /// Used for debug info.
        /// </summary>
        public string Name
        {
            get { return name ?? (name = this.GetType().Name); }
        }

        public abstract void DoRequest(GatewayConnection connection, DataPacket packet);

        public abstract void DoResponse(GatewayConnection connection, DataPacket packet);

        /// <summary>
        /// Returns the command name given a command id.
        /// </summary>
        /// <param name="commandId"></param>
        /// <returns></returns>
        internal static string GetCommandName(UInt16 commandId)
        {
            if (commandId < handlers.Length && handlers[commandId] != null)
                return handlers[commandId].Name;
            return "Unkown";
        }

        internal static bool IsAllowed(UInt16 command)
        {
            return (command < handlers.Length && handlers[command] != null);
        }
    }
}
