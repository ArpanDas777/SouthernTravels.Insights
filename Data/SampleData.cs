using System.Globalization;
using SouthernTravels.Insights.Infrastructure;
using SouthernTravels.Insights.Models;

namespace SouthernTravels.Insights.Data;

/// <summary>
/// Sample tickets and services, ported 1:1 from the design prototype.
/// Replace with a real repository when a data source is available.
/// </summary>
public static class SampleData
{
    /// <summary>The logged-in OPS user ("my files").</summary>
    public const string Me = "Priya Nair";
    public const string MeInitials = "PN";
    public const string MeRole = "OPS User · DEL001";

    /// <summary>The prototype's fixed "today".</summary>
    public static readonly DateTime Today = new(2026, 10, 5);

    private const string P = "Priya Nair", K = "Karan Shah";

    public static readonly IReadOnlyDictionary<string, Vendor> Vendors = new Dictionary<string, Vendor>
    {
        ["V101"] = new("V101", "Hotel Clarks Shiraz", "Accommodation"),
        ["V102"] = new("V102", "Royal Cabs Delhi", "Transport"),
        ["V103"] = new("V103", "Heritage Guides Jaipur", "Guide"),
        ["V104"] = new("V104", "Spice Route Caterers", "En-Route Meals"),
        ["V105"] = new("V105", "Taj Falaknuma Palace", "Accommodation"),
        ["V106"] = new("V106", "Deccan Travels", "Transport"),
        ["V107"] = new("V107", "Charminar Experiences", "Other"),
    };

    public static readonly IReadOnlyList<Ticket> Tickets = Build();

    /// <summary>Generated ticket numbers: GRP / SPL followed by four digits.</summary>
    private static string Tkn(int n) => (n % 2 == 1 ? "GRP" : "SPL") + (1000 + n * 73 % 8000);

    public static string VendorName(string id) => Vendors.TryGetValue(id, out var v) ? v.Name : id;

    private static DateTime D(string s) => DateTime.ParseExact(s, "dd MMM yyyy", CultureInfo.InvariantCulture);

    private static Service S(string type, string city, string name, string cat, string meal, string ops, string branch,
        string opsSt, string st, string? conf, string confNo, string start, string end, int days, string vendor,
        double sell, double payable, double vbal, int? tat) => new()
        {
            Type = type, City = city, Name = name, Category = cat, Meal = meal, Ops = ops, OpsBranch = branch,
            OpsStatus = opsSt, Status = st, Conf = conf is null ? null : D(conf), ConfNo = confNo,
            Start = D(start), End = D(end), Days = days, VendorId = vendor, Sell = sell, Payable = payable,
            VendorBalance = vbal, Tat = tat,
        };

    private static Ticket T(string no, string mbk, string file, string paxName, string tour, int pax, string status,
        string booked, string exec, string branch, string start, string end, int days, double grand, double paid,
        params Service[] svcs) => new()
        {
            No = no, Mbk = mbk, File = file, PaxName = paxName, Tour = tour, Pax = pax, Status = status,
            Booked = D(booked), Exec = exec, Branch = branch, Start = D(start), End = D(end), Days = days,
            Grand = grand, Paid = paid, Services = svcs,
        };

    private static List<Ticket> Build()
    {
        var t = new List<Ticket>
        {
            T("GRP4589", "MBK-88421", "FN/000001/2026-27/DEL001", "Rahul Mehta", "Golden Triangle Classic", 4, "Confirmed", "02 Sep 2026", "Anita Rao", "Delhi", "12 Oct 2026", "18 Oct 2026", 7, 148500, 148500,
                S("Accommodation", "Agra", "Hotel Clarks Shiraz – 2 Deluxe Rooms", "Deluxe", "MAP", P, "DEL001", "Completed", "Confirmed", "09 Sep 2026", "CN-70412", "14 Oct 2026", "16 Oct 2026", 2, "V101", 48000, 41500, 0, 4),
                S("Transport", "Delhi", "Innova Crysta – Full Tour", "SUV", "–", P, "DEL001", "Completed", "Confirmed", "08 Sep 2026", "CN-70388", "12 Oct 2026", "18 Oct 2026", 7, "V102", 52000, 44800, 0, 3),
                S("Guide", "Jaipur", "Licensed Guide – Amber Fort", "English", "–", P, "DEL001", "Completed", "Confirmed", "10 Sep 2026", "CN-70440", "16 Oct 2026", "16 Oct 2026", 1, "V103", 6500, 5200, 0, 5)),
            T("GRP2201", "MBK-88467", "FN/000001/2026-27/DEL001", "Sunita Iyer", "Hyderabad Heritage Weekend", 2, "Confirmed", "05 Sep 2026", "Vikram Das", "Hyderabad", "09 Oct 2026", "11 Oct 2026", 3, 96800, 40000,
                S("Accommodation", "Hyderabad", "Taj Falaknuma Palace – Suite", "Suite", "CP", K, "HYD001", "In Progress", "Confirmed", "20 Sep 2026", "CN-71102", "09 Oct 2026", "11 Oct 2026", 2, "V105", 72000, 61000, 61000, 15),
                S("Transport", "Hyderabad", "Airport Transfers + City Sedan", "Sedan", "–", K, "HYD001", "Completed", "Confirmed", "12 Sep 2026", "CN-71044", "09 Oct 2026", "11 Oct 2026", 3, "V106", 14800, 11900, 0, 7),
                S("Other", "Hyderabad", "Charminar Night Walk", "Experience", "–", K, "HYD001", "New", "Pending", null, "–", "10 Oct 2026", "10 Oct 2026", 1, "V107", 10000, 7600, 7600, null)),
            T("SPL5621", "MBK-88502", "FN/000001/2026-27/DEL001", "Arjun Kapoor", "Rajasthan Royal Circuit", 6, "Confirmed", "11 Sep 2026", "Anita Rao", "Delhi", "24 Oct 2026", "31 Oct 2026", 8, 312000, 100000,
                S("Accommodation", "Agra", "Hotel Clarks Shiraz – 3 Rooms", "Deluxe", "MAP", P, "DEL001", "New", "Pending", null, "–", "25 Oct 2026", "27 Oct 2026", 2, "V101", 96000, 82000, 82000, null),
                S("Transport", "Delhi", "Tempo Traveller – Full Tour", "Van", "–", P, "DEL001", "In Progress", "Confirmed", "22 Sep 2026", "CN-71390", "24 Oct 2026", "31 Oct 2026", 8, "V102", 88000, 74000, 74000, 11),
                S("En-Route Meals", "Jaipur", "Lunch – Chokhi Dhani Stop", "Veg", "Lunch", P, "DEL001", "New", "Pending", null, "–", "28 Oct 2026", "28 Oct 2026", 1, "V104", 18000, 14400, 14400, null)),
            T("SPL0430", "MBK-88519", "FN/000004/2026-27/DEL001", "Meera Pillai", "Delhi City Break", 2, "Confirmed", "14 Sep 2026", "Rohit Sen", "Delhi", "06 Oct 2026", "08 Oct 2026", 3, 54200, 54200,
                S("Accommodation", "Delhi", "Hotel Clarks Shiraz – Deluxe Room", "Deluxe", "EP", K, "HYD001", "Completed", "Confirmed", "18 Sep 2026", "CN-71201", "06 Oct 2026", "08 Oct 2026", 2, "V101", 30000, 25500, 0, 4),
                S("Guide", "Delhi", "Old Delhi Heritage Guide", "Hindi", "–", K, "HYD001", "Pending", "Confirmed", "19 Sep 2026", "CN-71233", "07 Oct 2026", "07 Oct 2026", 1, "V103", 4500, 3600, 3600, null)),
            T("GRP0433", "MBK-88540", "FN/000005/2026-27/HYD001", "Farhan Qureshi", "Deccan Express", 3, "Cancelled", "15 Sep 2026", "Vikram Das", "Hyderabad", "15 Oct 2026", "19 Oct 2026", 5, 67000, 0,
                S("Accommodation", "Hyderabad", "Taj Falaknuma Palace – 2 Rooms", "Deluxe", "MAP", K, "HYD001", "New", "Cancelled", null, "–", "15 Oct 2026", "19 Oct 2026", 4, "V105", 55000, 46000, 0, null),
                S("Transport", "Hyderabad", "Deccan Travels – Sedan 5 Days", "Sedan", "–", K, "HYD001", "New", "Cancelled", null, "–", "15 Oct 2026", "19 Oct 2026", 5, "V106", 12000, 9800, 0, null)),
            T("GRP8843", "MBK-88561", "FN/000001/2026-27/DEL001", "Neha Bansal", "Jaipur Palaces", 5, "Confirmed", "19 Sep 2026", "Rohit Sen", "Delhi", "02 Nov 2026", "05 Nov 2026", 4, 128400, 128400,
                S("Accommodation", "Jaipur", "Hotel Clarks Shiraz – 3 Rooms", "Deluxe", "MAP", P, "DEL001", "Completed", "Confirmed", "24 Sep 2026", "CN-71544", "02 Nov 2026", "05 Nov 2026", 3, "V101", 84000, 70500, 0, 5),
                S("Guide", "Jaipur", "Licensed Guide – City Palace", "English", "–", P, "DEL001", "Completed", "Confirmed", "25 Sep 2026", "CN-71560", "03 Nov 2026", "03 Nov 2026", 1, "V103", 6400, 5100, 0, 4)),
            T("GRP9012", "MBK-88603", "FN/000007/2026-27/DEL001", "Kavita Menon", "Royal Rajasthan Grand Tour", 4, "Confirmed", "21 Sep 2026", "Anita Rao", "Delhi", "20 Oct 2026", "26 Oct 2026", 7, 276500, 150000,
                S("Accommodation", "Jaipur", "Hotel Clarks Shiraz – 2 Suites", "Suite", "MAP", P, "DEL001", "Completed", "Confirmed", "27 Sep 2026", "CN-71702", "21 Oct 2026", "24 Oct 2026", 3, "V101", 112000, 94000, 0, 6),
                S("Transport", "Delhi", "Innova Crysta – Full Tour", "SUV", "–", P, "DEL001", "Completed", "Confirmed", "26 Sep 2026", "CN-71688", "20 Oct 2026", "26 Oct 2026", 7, "V102", 64000, 53500, 0, 4),
                S("En-Route Meals", "Jaipur", "Lunch – Highway Heritage Stop", "Veg", "Lunch", P, "DEL001", "In Progress", "Confirmed", "29 Sep 2026", "CN-71731", "22 Oct 2026", "22 Oct 2026", 1, "V104", 16500, 13200, 13200, null),
                S("Guide", "Jaipur", "Licensed Guide – Amber Fort & City Palace", "English", "–", P, "DEL001", "Completed", "Confirmed", "28 Sep 2026", "CN-71715", "22 Oct 2026", "23 Oct 2026", 2, "V103", 14000, 11200, 0, 5),
                S("Other", "Jaipur", "Elephant Village Experience", "Experience", "–", P, "DEL001", "New", "Pending", null, "–", "23 Oct 2026", "23 Oct 2026", 1, "V107", 70000, 58000, 58000, null)),
        };

        // 12 generated tickets derived from the first seven (dates shifted).
        string[] names = { "Deepak Verma", "Ritu Sharma", "Imran Sheikh", "Lakshmi Nair", "Vivek Gupta", "Pooja Desai", "Sandeep Rao", "Anjali Joshi", "Nikhil Bhat", "Shweta Kulkarni", "Tarun Malhotra", "Divya Reddy" };
        for (var i = 0; i < 12; i++)
        {
            var b = t[i % 7];
            var n = 4 + i * 3;
            t.Add(b with
            {
                No = Tkn(610 + i),
                Mbk = "MBK-" + (88700 + i),
                File = "FN/" + (8 + i).ToString("D6") + "/2026-27/" + (i % 2 == 1 ? "HYD001" : "DEL001"),
                PaxName = names[i],
                Booked = b.Booked.AddDays(3),
                Start = b.Start.AddDays(n),
                End = b.End.AddDays(n),
                Services = b.Services.Select(x => x with { Start = x.Start.AddDays(n), End = x.End.AddDays(n) }).ToList(),
            });
        }

        // Two extra cancelled tickets.
        string[] cancelledNames = { "Gopal Krishnan", "Aarti Saxena" };
        for (var k = 0; k < 2; k++)
        {
            var b = t[k];
            var n = 30 + k * 9;
            t.Add(b with
            {
                No = Tkn(630 + k),
                Mbk = "MBK-" + (88740 + k),
                File = "FN/" + (20 + k).ToString("D6") + "/2026-27/DEL001",
                PaxName = cancelledNames[k],
                Status = "Cancelled",
                Booked = b.Booked.AddDays(2),
                Start = b.Start.AddDays(n),
                End = b.End.AddDays(n),
                Services = b.Services.Select(x => x with
                {
                    Status = "Cancelled", OpsStatus = "New", Conf = null, ConfNo = "–", VendorBalance = 0, Tat = null,
                    Start = x.Start.AddDays(n), End = x.End.AddDays(n),
                }).ToList(),
            });
        }

        // 21 confirmed tickets (three per vendor V101–V107) so every vendor has dated activity around today.
        string[] vendorPax = { "Sanjay Kulkarni", "Fatima Sheikh", "Rajiv Menon", "Isha Agarwal", "Manoj Pillai", "Zoya Khan", "Harsh Vora", "Naina Kapoor", "Alok Dubey", "Revathi Iyer", "Bhavna Shah", "Yash Chopra", "Tanya Bose", "Omkar Patil", "Sana Mirza", "Kunal Arora", "Deepti Rao", "Vinay Hegde", "Sheetal Jain", "Arvind Nambiar", "Pallavi Sen" };
        int[] baseTicket = { 0, 1, 2, 3, 5 };
        var c = 0;
        for (var vi = 0; vi < 7; vi++)
        {
            var vid = "V10" + (vi + 1);
            var tpl = t.SelectMany(x => x.Services).First(x => x.VendorId == vid);
            for (var k = 0; k < 3; k++)
            {
                var b = t[baseTicket[(vi + k) % 5]];
                var n = -25 + k * 24 + vi * 4;
                var f = 0.8 + k * 0.2;
                var payable = Fmt.Round(tpl.Payable * f / 100) * 100;
                t.Add(b with
                {
                    No = Tkn(650 + c),
                    Mbk = "MBK-" + (88800 + c),
                    File = "FN/" + (30 + c).ToString("D6") + "/2026-27/" + ((vi + k) % 2 == 1 ? "HYD001" : "DEL001"),
                    PaxName = vendorPax[c % vendorPax.Length],
                    Status = "Confirmed",
                    Booked = b.Booked.AddDays(n - 10),
                    Start = b.Start.AddDays(n),
                    End = b.End.AddDays(n),
                    Services = new[] { tpl with { Status = "Confirmed", Start = tpl.Start.AddDays(n), End = tpl.End.AddDays(n), Sell = Fmt.Round(tpl.Sell * f / 100) * 100, Payable = payable, VendorBalance = k == 1 ? payable : 0 } },
                });
                c++;
            }
        }

        // Only GRP9012 stays assigned to the logged-in user; everything else moves to Karan Shah.
        return t.Select((x, i) => x with
        {
            Index = i,
            Services = x.No == "GRP9012" ? x.Services : x.Services.Select(s => s.Ops == Me ? s with { Ops = K } : s).ToList(),
        }).ToList();
    }
}
