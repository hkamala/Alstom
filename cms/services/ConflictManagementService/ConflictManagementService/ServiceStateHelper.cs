using System;
using System.Collections.Generic;

namespace E2KService;
internal class ServiceStateHelper
{
    // Shutdown is special service state (not sent by watchdog)
    public enum ServiceState { Shutdown, Offline, ReadyForStandby, Standby, ReadyForOnline, Online, OnlineDegraded }

    private static Dictionary<ServiceState, string> c_serviceStates = new()
    {
        { ServiceState.Offline, "Offline" },
        { ServiceState.ReadyForStandby, "ReadyForStandby" },
        { ServiceState.Standby, "StandBy" },
        { ServiceState.ReadyForOnline, "ReadyForOnline" },
        { ServiceState.Online, "Online" },
        { ServiceState.OnlineDegraded, "OnlineDegraded" }
    };

    ////////////////////////////////////////////////////////////////////////////////

    public static string GetState(ServiceState state)
    {
        return c_serviceStates[state];
    }

    public static ServiceState GetState(string state)
    {
        foreach (var key in c_serviceStates.Keys)
        {
            if (c_serviceStates[key] == state)
                return key;
        }

        throw new ArgumentException("<state>", "Process state is not valid: " + state);
    }
}

