using System.Runtime.InteropServices;
using System.Threading.Tasks.Dataflow;
using System.IO;
using System.Numerics;
using System.Formats.Asn1;
using System.Globalization;
using CsvHelper;
using System.Xml.Linq;
using System.Collections.Generic;
using System.Reflection.PortableExecutable;
using System.Diagnostics.Tracing;

namespace AliceDataPairerV2
{
    /// <summary>
    /// creates necssary legible files  
    /// make sure to delete files before running this
    /// </summary>
    internal class Program
    {
        static void Main(string[] args)
        {
            //StreamReader reader5 = new StreamReader("M:\\divin\\ReposHard\\AliceData\\BERTdata.tsv");
            //string blarg = reader5.ReadLine().Replace("1\t", "").Replace("0\t", "");
            //Console.WriteLine(blarg);
            //take in outcomes
            StreamReader reader = File.OpenText("M:\\divin\\ReposHard\\AliceData\\application_outcomes.txt");
            string line;
            Dictionary<string, v2tuple> v2data = new Dictionary<string, v2tuple>();
            Dictionary<string, v2tuple> preData = new Dictionary<string, v2tuple>();
            bool first = true;
            while ((line = reader.ReadLine()) != null)
            {
                if (first)
                {
                    first = false;
                    continue;
                }
                //split lines by whitespace
                string[] items = line.Split(',');
                //id is at items[0] and filing date is at items[6], granted is at items[4]
                int timeflag = dateThreshold(items[6]);
                if (timeflag == 2)
                    v2data.Add(items[0], new v2tuple(int.Parse(items[4])));
                else if (timeflag == 1)
                    preData.Add(items[0], new v2tuple(int.Parse(items[4])));

            }
            reader.Dispose();

            //read the crosswalk data
            Dictionary<string, string> PgsToApps = buildCrosswalk("M:\\divin\\ReposHard\\AliceData\\pg_granted_patent_crosswalk.tsv", v2data, preData);

            //take in claim description data from 2014-2023 (all seperate files)
            //this data is formatted with the id, and then some unimpotant text, and then summary of the invenction
            //this file needs to be parsed in such a way that I can extract the id from the first line, and then parse until I hit the summary
            for(int i = 10; i<15; i++)
            {
                StreamReader reader3 = new StreamReader("M:\\divin\\ReposHard\\AliceData\\descriptions\\pg_brf_sum_text_20" + i + ".tsv");
                while ((line = reader3.ReadLine()) != null)
                {
                    line = line.Replace("\"", "");
                    //check if the line has the id
                    if (line.Length > 4 && int.TryParse(line.Substring(0, 4), out int k))
                    {
                        string[] items = line.Split("\t");
                        items[0] += ".0";
                        //items[0] should containt the publication id. 
                        if (PgsToApps.TryGetValue(items[0], out string key))
                        {
                            string summary = "";
                            while (!line.ToLower().Contains("ummary"))
                                line = reader3.ReadLine();
                            line = reader3.ReadLine();
                            while (!line.Equals("\""))
                            {
                                if (string.IsNullOrWhiteSpace(line))
                                {
                                    line = reader3.ReadLine();
                                    continue;
                                }
                                summary += " " + line;
                                line = reader3.ReadLine();
                            }
                            //add the summary to the hashmap
                            summary = summary.Replace("\t", " ");
                            if (summary.Length < 30) //not long enough to be used
                                continue;
                            try
                            {
                                preData[PgsToApps[items[0]]].descripion = summary;
                            }
                            catch
                            {
                                Console.WriteLine("dupe"); //a duplicate entry has been added
                                continue;
                            }
                            //write to file and remove it from memory

                            writeEntryToFile(PgsToApps[items[0]], preData[PgsToApps[items[0]]], "M:\\divin\\ReposHard\\AliceData\\BERTdataPreAlice.tsv");
                            preData.Remove(PgsToApps[items[0]]);
                            PgsToApps.Remove(items[0]);
                        }
                    }
                }
            }



            for (int i = 14; i < 23; i++)
            {
                StreamReader reader3 = new StreamReader("M:\\divin\\ReposHard\\AliceData\\descriptions\\pg_brf_sum_text_20" + i + ".tsv");
                while ((line = reader3.ReadLine()) != null)
                {
                    line = line.Replace("\"", "");
                    //check if the line has the id
                    if (line.Length > 4 && int.TryParse(line.Substring(0, 4), out int k))
                    {
                        string[] items = line.Split("\t");
                        items[0] += ".0";
                        //items[0] should containt the publication id. 
                        if (PgsToApps.TryGetValue(items[0], out string key))
                        {
                            string summary = "";
                            while (!line.ToLower().Contains("ummary"))
                                line = reader3.ReadLine();
                            line = reader3.ReadLine();
                            while (!line.Equals("\""))
                            {
                                if (string.IsNullOrWhiteSpace(line))
                                {
                                    line = reader3.ReadLine();
                                    continue;
                                }
                                summary += " " + line;
                                line = reader3.ReadLine();
                            }
                            //add the summary to the hashmap
                            summary = summary.Replace("\t", " ");
                            if (summary.Length < 30) //not long enough to be used
                                continue;
                            try
                            {
                                v2data[PgsToApps[items[0]]].descripion = summary;
                            }
                            catch
                            {
                                Console.WriteLine("dupe"); //a duplicate entry has been added
                                continue;
                            }
                            //write to file and remove it from memory

                            writeEntryToFile(PgsToApps[items[0]], v2data[PgsToApps[items[0]]], "M:\\divin\\ReposHard\\AliceData\\BERTdata.tsv");
                            v2data.Remove(PgsToApps[items[0]]);
                            PgsToApps.Remove(items[0]);
                        }
                    }
                }
            }
        }


        /// <summary>
        /// this method will write the key and v1 tuple to the file 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="tup"></param>
        public static void writeEntryToFile(string key, v2tuple tup, string fileName)
        {
            try
            {
                using (System.IO.StreamWriter file = new System.IO.StreamWriter(fileName, true))
                {
                    file.WriteLine(key + "\t" + tup.approved + "\t" + tup.descripion);

                }
            }
            catch
            {
                Console.WriteLine("fuck");
            }
        }
        /// <summary>
        /// this method takes a line and writes it to a file
        /// </summary>
        /// <param name="key"></param>
        /// <param name="tup"></param>
        public static void writeEntryToFile(string line, string fileName)
        {
            try
            {
                using (System.IO.StreamWriter file = new System.IO.StreamWriter(fileName, true))
                {
                    file.WriteLine(line);

                }
            }
            catch
            {
                Console.WriteLine("fuck");
            }
        }


        /// <summary>
        /// this class represents a tuple for stroing data for the v1 model.
        /// </summary>
        public class v2tuple
        {

            public int approved { get; set; }
            public string descripion { get; set; }
            public v2tuple(int _approved)
            {
                this.approved = _approved;
                this.descripion = "";
            }
        }



        /// <summary>
        /// this method reads the crosswalk from the filepath, and if the values in it correspond to either dataset, add it to the returned dictionary.
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="data1"></param>
        /// <param name="data2"></param>
        /// <returns></returns>
        public static Dictionary<string, string> buildCrosswalk(string filename, Dictionary<string, v2tuple> data1, Dictionary<string, v2tuple> data2 )
        {
            //get the crosswalk data to use application ids and map them to pgpubids
            //read the text
            StreamReader reader2 = new StreamReader("M:\\divin\\ReposHard\\AliceData\\pg_granted_patent_crosswalk.tsv");
            bool first = true;
            Dictionary<string, string> PgsToApps = new();
            int count = 0;
            string line = "stub";
            //seperate it 
            while ((line = reader2.ReadLine()) != null)
            {
                if (first)
                {
                    first = false;
                    continue;
                }
                string[] items = line.Replace("\"", "").Split("\t");
                //if its not in either sets of data, its not necessary
                if ((!data1.TryGetValue(items[1], out v2tuple? tup)) && (!data2.TryGetValue(items[1], out v2tuple? tup2)))
                    continue;
                if (items[0] != "")
                    try
                    {
                        PgsToApps.Add(items[0], items[1]);
                    }
                    catch
                    {
                        //stub
                        count++;
                    }
            }
            //Console.WriteLine(count);
            reader2.Dispose();
            return PgsToApps;
        }



        /// <summary>
        /// determines a dates compariosn to 2010 and june 19th 2014
        /// </summary>
        /// <param name="date"></param>
        /// <returns> returns 0 if before 2010, 1 if after 2010 before alice, returns 2 if after alic
        /// </returns>
        public static int dateThreshold (string date)
        {
            if (string.IsNullOrEmpty(date))
                return 0;
            int year = int.Parse(date.Substring(5, 4));
            if (year < 2010)
                return 0;
            if(year > 2014)
                return 2;
            //at this point the year must be 2014
            string month = date.Substring(2, 2);
            if (month.Equals("jul") || month.Equals("aug") || month.Equals("sep") || month.Equals("oct") || month.Equals("nov") || month.Equals("dec"))
                return 2;
            if( month.Equals("jan") || month.Equals("feb") || month.Equals("mar") || month.Equals("apr") || month.Equals("may"))
                return 1;
            //the month is now june
            int day = int.Parse(date.Substring(0, 2));
            if (day < 20)
                return 1;
            return 2;
        }    
    
    }
}