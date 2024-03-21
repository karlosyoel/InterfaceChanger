// See https://aka.ms/new-console-template for more information
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Text.RegularExpressions;

var lastIface = "";

var go = true;
while (go)
{
    var list = getInterfaces();
    if (list != null)
    {
        lastIface = await processData(list, lastIface);
    }
}



Console.ReadKey();
static async Task<string> processData(List<Interface> list,string lastIface)
{
    var exTime = @"(?<=time=)(.*?)(?=ms)";
    var regTime = new Regex(exTime, RegexOptions.IgnoreCase);
    var max = 10000;
    var interf = "";
    
    var errors = 0;

    foreach (var iface in list)
    {
        var cmd = $"ping 1.1.1.1 -n 3 -S {iface.ip} -i 25 | findstr /R 'TTL'";
        var r = runCmd(cmd);

        if (!string.IsNullOrEmpty(r))
        {
            var resp = regTime.Matches(r);
            var ttl = 0;
            foreach (var item in resp)
            {
                ttl += int.Parse(item?.ToString() ?? "0");
            }
            if (max > ttl)
            {
                max = ttl;
                interf = iface.name;
            }
        }
    }
    if (!string.IsNullOrEmpty(interf) && list.Count > 1)
    {
        var i = 50;
        var nl = list.Where(x => x.name != interf);

        if (lastIface != interf)
        {
            lastIface = interf;
            foreach (var iface in nl)
            {
                setInterface(iface.name, i++);
            }
            setInterface(interf, 1);
            await Console.Out.WriteLineAsync($"Set {interf}");
        }
        else
        {
            await Console.Out.WriteLineAsync("No change");
        }
    }
    return interf;
}

static void setInterface(string name, int metric)
{
    runCmd($"Set-NetIPInterface -InterfaceAlias '{name}' -InterfaceMetric {metric}");
}

static List<Interface> getInterfaces()
{
    var output = runCmd("ipconfig");
    var lines = output.Split('\n');

    var exName = @"(?<=adapter\s)(.*?)(?=\:)";
    var regName = new Regex(exName, RegexOptions.IgnoreCase);

    var exIp = @"(?<=IPv4(.*?:)\s)(.*?)(?=$)";
    var regIp = new Regex(exIp, RegexOptions.IgnoreCase);

    bool buscarIp = false;
    var name = "";
    var ip1 = "";
    var interfaces = new List<Interface>();
    foreach (var line in lines)
    {
        if (buscarIp && regIp.IsMatch(line))
        {
            ip1 = regIp.Match(line).Value;
            interfaces.Add(new()
            {
                name = name,
                ip = ip1.TrimEnd()
            });
        }
        if (regName.IsMatch(line))
        {
            buscarIp = true;
            name = regName.Match(line).Value;
        }
    }
    return interfaces;
}

static string runCmd(string cmd)
{
    Process process = new Process();
    process.StartInfo.FileName = "powershell.exe";
    process.StartInfo.Arguments = $"/c {cmd}";
    process.StartInfo.CreateNoWindow = true;
    process.StartInfo.RedirectStandardOutput = true;
    process.Start();
    process.WaitForExit();

    var dd = process.StandardOutput.ReadToEnd();
    return dd;
}

class Interface
{
    public string name { get; set; }
    public string ip { get; set; }
}

