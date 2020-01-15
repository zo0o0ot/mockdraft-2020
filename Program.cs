using CsvHelper;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace mockdraft_2020
{
    class Program
    {
        static void Main(string[] args)
        {
            var webGet = new HtmlWeb();
            webGet.UserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64; rv:31.0) Gecko/20100101 Firefox/31.0";
            var document1 = webGet.Load("https://www.drafttek.com/2020-NFL-Mock-Draft/2020-NFL-Mock-Draft-Round-1.asp");
            var document2 = webGet.Load("https://www.drafttek.com/2020-NFL-Mock-Draft/2020-NFL-Mock-Draft-Round-1b.asp");
            var document3 = webGet.Load("https://www.drafttek.com/2020-NFL-Mock-Draft/2020-NFL-Mock-Draft-Round-2.asp");
            var document4 = webGet.Load("https://www.drafttek.com/2020-NFL-Mock-Draft/2020-NFL-Mock-Draft-Round-3.asp");
            var document5 = webGet.Load("https://www.drafttek.com/2020-NFL-Mock-Draft/2020-NFL-Mock-Draft-Round-4.asp");
            var document6 = webGet.Load("https://www.drafttek.com/2020-NFL-Mock-Draft/2020-NFL-Mock-Draft-Round-6.asp");

            //Console.WriteLine(document1.ParsedText);
            //#content > table:nth-child(9)
            //html body div#outer div#wrapper2 div#content table
            ///html/body/div[3]/div[3]/div[1]/table[1]
            
            
            // Need to get date of mock draft eventually.
            string draftDate = getDraftDate(document1);

            List<MockDraftPick> list1 = getMockDraft(document1, draftDate);
            List<MockDraftPick> list2 = getMockDraft(document2, draftDate);
            List<MockDraftPick> list3 = getMockDraft(document3, draftDate);
            List<MockDraftPick> list4 = getMockDraft(document4, draftDate);
            List<MockDraftPick> list5 = getMockDraft(document5, draftDate);
            List<MockDraftPick> list6 = getMockDraft(document6, draftDate);

            //This is the file name we are going to write.
            var csvFileName = $"mocks{Path.DirectorySeparatorChar}{draftDate}-mock.csv";

            Console.WriteLine("Creating csv...");

            //Write projects to csv with date.
            using (var writer = new StreamWriter(csvFileName))
            using (var csv = new CsvWriter(writer))
            {
                csv.Configuration.RegisterClassMap<MockDraftPickMap>();
                csv.WriteRecords(list1);
                csv.WriteRecords(list2);
                csv.WriteRecords(list3);
                csv.WriteRecords(list4);
                csv.WriteRecords(list5);
                csv.WriteRecords(list6);
            }






            // Document data is of type HtmlAgilityPack.HtmlDocument - need to parse it to find info.
            // I'm pretty sure I'm looking for tables with this attribute: background-image: linear-gradient(to bottom right, #0b3661, #5783ad);

            
            Console.WriteLine("Behold, the draft!");

            //launch.js-

            // {
            //     // Use IntelliSense to find out which attributes exist for C# debugging
            //     // Use hover for the description of the existing attributes
            //     // For further information visit https://github.com/OmniSharp/omnisharp-vscode/blob/master/debugger-launchjson.md
            //     "version": "0.2.0",
            //     "configurations": [
            //             {
            //                 "name": ".NET Core Launch (console)",
            //                 "type": "coreclr",
            //                 "request": "launch",
            //                 "preLaunchTask": "build",
            //                 // If you have changed target frameworks, make sure to update the program path.
            //                 "program": "${workspaceFolder}/bin/Debug/netcoreapp3.1/linux-x64/mockdraft-2020.dll",
            //                 "args": [],
            //                 "cwd": "${workspaceFolder}",
            //                 // For more information about the 'console' field, see https://aka.ms/VSCode-CS-LaunchJson-Console
            //                 "console": "internalConsole",
            //                 "stopAtEntry": false
            //             },
            //             {
            //                 "name": ".NET Core Attach",
            //                 "type": "coreclr",
            //                 "request": "attach",
            //                 "processId": "${command:pickProcess}"
            //             }
            //         ]
            //     }
        }
        public static List<MockDraftPick> getMockDraft(HtmlAgilityPack.HtmlDocument doc, string pickDate)
        {
            List<MockDraftPick> mdpList = new List<MockDraftPick>();
            // This is still messy from debugging the different values.  It should be optimized.
            var dn = doc.DocumentNode;
            var dns = dn.SelectNodes("/html/body/div/div/div/table");
            var attr = dns[1].Attributes;
            var attrs = attr.ToArray();
            var style = attr.FirstOrDefault().Value;
            var ss = style.ToString();
            bool hasStyle = ss.IndexOf("background-image: linear-gradient", StringComparison.OrdinalIgnoreCase) >= 0;
            foreach(var node in dns)
            {
                var nodeStyle = node.Attributes.FirstOrDefault().Value.ToString();
                bool hasTheStyle = node.Attributes.FirstOrDefault().Value.ToString().IndexOf("background-image: linear-gradient", StringComparison.OrdinalIgnoreCase) >= 0;
                if (hasTheStyle)
                {
                    var tr = node.SelectSingleNode("tr");
                    MockDraftPick mockDraftPick = createMockDraftEntry(tr, pickDate);
                    mdpList.Add(mockDraftPick);
                }
            }
            var hasGradient = dns[1].Attributes.Contains("background-image");
            return mdpList;
        }
        public static MockDraftPick createMockDraftEntry(HtmlNode tableRow, string pickDate)
        {
            var childNodes = tableRow.ChildNodes;
            var node1 = childNodes[1].InnerText; //pick number?
            string pickNumber = node1.Replace("\r","")
                                    .Replace("\n","")
                                    .Replace("\t","")
                                    .Replace(" ","");
            var node3 = childNodes[3]; //team (and team image)?
            var teamCity = node3.ChildNodes[0].InnerText
                                    .Replace("\r","")
                                    .Replace("\n","")
                                    .Replace("\t","")
                                    .TrimEnd();
            var node5 = childNodes[5]; //Has Child Nodes - Player, School, Position, Reach/Value
            string playerName = node5.ChildNodes[1].InnerText
                                    .Replace("\r","")
                                    .Replace("\n","")
                                    .Replace("\t","")
                                    .TrimEnd();
            string playerSchool = node5.ChildNodes[3].InnerText
                                    .Replace("\r","")
                                    .Replace("\n","")
                                    .Replace("\t","")
                                    .TrimEnd(); // this may have a space afterwards.
            string playerPosition = node5.ChildNodes[5].InnerText
                                    .Replace("\r","")
                                    .Replace("\n","")
                                    .Replace("\t","")
                                    .Replace(" ","");
            string reachValue = node5.ChildNodes[9].InnerText
                                    .Replace("\r","")
                                    .Replace("\n","")
                                    .Replace("\t","")
                                    .Replace(" ","");
            
            
            MockDraftPick mdp = new MockDraftPick(pickNumber, teamCity, playerName, playerSchool, playerPosition, reachValue, pickDate);
            Console.WriteLine(mdp.round);
            Console.WriteLine(mdp.leagifyPoints);
            Console.WriteLine(mdp.pickNumber);
            Console.WriteLine(mdp.teamCity);
            Console.WriteLine(mdp.playerName);
            Console.WriteLine(mdp.school);
            Console.WriteLine(mdp.position);
            Console.WriteLine(mdp.reachValue);
            return mdp;
        }
        public static string getDraftDate(HtmlAgilityPack.HtmlDocument doc)
        {
            HtmlNode hn = doc.DocumentNode;
            HtmlNode hi1 = hn.SelectSingleNode("//*[@id='HeadlineInfo1']");
            Console.WriteLine(hi1.InnerText);
            string hi2 = hi1.InnerText.Replace(" EST", "").Trim();
            //Change date to proper date. The original format should be like this:
            //" May 21, 2019 2:00 AM EST"
            DateTime parsedDate;
            bool parseWorks = DateTime.TryParse(hi2, out parsedDate);
            string dateInNiceFormat = "";
            if (parseWorks)
            {
                dateInNiceFormat = parsedDate.ToString("yyyy-MM-dd");
            }
            else
            {
                dateInNiceFormat = DateTime.Now.ToString("yyyy-MM-dd");
            }
            
            Console.WriteLine("Date parsed: " + parsedDate + " DateTime parse worked?: " + parseWorks + "date output" + dateInNiceFormat);
            return dateInNiceFormat;
        }
    }
}
