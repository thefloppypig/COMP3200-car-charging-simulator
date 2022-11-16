using System.Collections.Generic;
using CodingConnected.TraCI.NET.Helpers;
using CodingConnected.TraCI.NET.Types;

namespace CodingConnected.TraCI.NET.Commands
{
    public class ChargingStationCommands : TraCICommandsBase
    {
        #region Public Commands
        /// <summary>
        /// Returns the lane of this calibrator (if it applies to a single lane)
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public TraCIResponse<string> GetLaneID(string id)
        {
            return
                TraCICommandHelper.ExecuteGetCommand<string>(
                    Client,
                    id,
                    TraCIConstants.CMD_GET_CHARGINGSTATION_VARIABLE,
                    TraCIConstants.VAR_LANE_ID);
        }

        /// <summary>
        /// The starting position of the stop along the lane measured in m.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public TraCIResponse<double> GetStartPos(string id)
        {
            return
                TraCICommandHelper.ExecuteGetCommand<double>(
                    Client,
                    id,
                    TraCIConstants.CMD_GET_CHARGINGSTATION_VARIABLE,
                    TraCIConstants.VAR_POSITION);
        }

        /// <summary>
        /// The end position of the stop along the lane measured in m.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public TraCIResponse<double> GetEndPos(string id)
        {
            return
                TraCICommandHelper.ExecuteGetCommand<double>(
                    Client,
                    id,
                    TraCIConstants.CMD_GET_CHARGINGSTATION_VARIABLE,
                    TraCIConstants.VAR_LANEPOSITION);
        }

        /// <summary>
        /// Returns the name of this stop
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public TraCIResponse<string> GetName(string id)
        {
            return
                TraCICommandHelper.ExecuteGetCommand<string>(
                    Client,
                    id,
                    TraCIConstants.CMD_GET_CHARGINGSTATION_VARIABLE,
                    TraCIConstants.VAR_STREET_NAME);
        }

        /// <summary>
        /// Get the total number of vehicles stopped at the named charging station.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public TraCIResponse<int> GetVehicleCount(string id)
        {
            return
                TraCICommandHelper.ExecuteGetCommand<int>(
                    Client,
                    id,
                    TraCIConstants.CMD_GET_CHARGINGSTATION_VARIABLE,
                    TraCIConstants.VAR_STOP_STARTING_VEHICLES_NUMBER);
        }

        /// <summary>
        /// Get the IDs of vehicles stopped at the named charging station.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public TraCIResponse<List<string>> GetIdList(string id)
        {
            return
                TraCICommandHelper.ExecuteGetCommand<List<string>>(
                    Client,
                    id,
                    TraCIConstants.CMD_GET_CHARGINGSTATION_VARIABLE,
                    TraCIConstants.VAR_STOP_STARTING_VEHICLES_IDS);
        }


        #endregion 
        #region Constructor
        public ChargingStationCommands(TraCIClient client) : base(client)
        {
        }
        #endregion // Constructor
    }
}
