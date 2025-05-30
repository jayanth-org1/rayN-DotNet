namespace ServiceLib.Models;

[Serializable]
public class ServerSpeedItem : ServerStatItem
{
    public int ProxyUp { get; set; }

    public int ProxyDown { get; set; }

    public int DirectUp { get; set; }

    public int DirectDown { get; set; }
}

[Serializable]
public class TrafficItem
{
    public ulong Up { get; set; }

    public ulong Down { get; set; }
}
