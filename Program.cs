
using Common;
using GeneticAlgorithm_TravelingSalesmanCsharp.Concrete;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace GeneticAlgorithm_TravelingSalesmanCsharp
{
    class Program
    {
        static List<TravelingSpecimen> candidates {get;set;} = new List<TravelingSpecimen>();
        static List<Node> nodes {get;set;} = new List<Node>();

        static int maxThreads = Math.Max(Environment.ProcessorCount,1);
        static int currentThreads = 0;
        static int genCount = 0;
        static Worker<Tuple<TravelingSpecimen,TravelingSpecimen>,TravelingSpecimen[]>[] workers;

        static decimal fitness(TravelingSpecimen specimen) {
            decimal fit = specimen.distance();
            return fit;
        }

        static void beginTests(List<Node> _nodes = null, List<TravelingSpecimen> _cand = null) {
            string templateDataPath = "data{0}.csv";
            string templateOutputPath = "output{0}.txt";

            int index = 0;
            while (File.Exists(string.Format(templateDataPath,index))) {
                index++;
            }
            string dataPath = string.Format(templateDataPath,index);

            index=0;
            while (File.Exists(string.Format(templateOutputPath,index))) {
                index++;
            }
            string outputPath = string.Format(templateOutputPath,index);

            StreamWriter swData = new StreamWriter(dataPath);
            StreamWriter swOutput = new StreamWriter(outputPath);

            Console.WriteLine("Beginning Tests");
            initNodes(_nodes);
            initCandidates(_cand);

            string output_header = "Generation\tMax Fit\tMin Fit\tAverage Fit\tCandidates\tTime\n";
            string data_header = "Generation,Id,Fitness,Mutation Count,Parent A,Parent B";

            index = 0;
            foreach (Node v in candidates[0].value) {
                data_header += string.Format(",Value {0}",index);
                index++;
            }
            data_header += "\n";

            swData.Write(data_header);
            swOutput.Write(output_header);

            string output = "";

            foreach (TravelingSpecimen c in candidates) {
                output += string.Format("{0},{1},{2},{3},{4},",0,c.id,c.fit,c.mutationCount,c.parentIdA,c.parentIdB);
                output += string.Join(",",c.value);
                output += "\n";
            }

            swData.Write(output);

            initWorkers();
            decimal? maxFit = null;
            int sameCount = 0;
            int lastImproved = 0;
            Console.WriteLine(output_header);
            Tuple<decimal,decimal,decimal> results = null;
            while (sameCount<100000) {
                genCount++;
                results = createCandidates();
                if (!maxFit.HasValue || results.Item1<maxFit.Value) {
                    output = "";
                    foreach (TravelingSpecimen c in candidates) {
                        output += string.Format("{0},{1},{2},{3},{4},{5},",genCount,c.id,c.fit,c.mutationCount,c.parentIdA,c.parentIdB);
                        output += string.Join(",",c.value);
                        output += "\n";
                    }

                    swData.Write(output);

                    lastImproved = genCount;
                    bool display = (!maxFit.HasValue || Math.Round(results.Item1,6)<Math.Round(maxFit.Value,6));
                    maxFit = results.Item1;
                    sameCount=0;
                    if (display) {
                        output = string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}",genCount,Math.Round(maxFit.Value,6),Math.Round(results.Item2,6),Math.Round(results.Item3,6),candidates.Count,DateTime.Now);
                        swOutput.Write(output + '\n');
                        Console.WriteLine(output);
                    }
                } else if (results.Item1 == maxFit) {
                    sameCount++;
                }

                if (genCount%10000==0) {
                    output = string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}",genCount,Math.Round(maxFit.Value,6),Math.Round(results.Item2,6),Math.Round(results.Item3,6),candidates.Count,DateTime.Now);
                    swOutput.Write(output + "\n");
                    Console.WriteLine(output);
                }
            }

            if (results!=null) {
                output = string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}",genCount,Math.Round(maxFit.Value,6),Math.Round(results.Item2,6),Math.Round(results.Item3,6),candidates.Count,DateTime.Now);
                swOutput.Write(output + "\n");
                Console.WriteLine(output);
            }
            

            output = "Best Candidates:";
            for (int i=0; i<3 && i<candidates.Count; i++) {
                output += string.Format("\nRank {0}\t{1}",i+1,candidates[i]);
            }

            swOutput.Write(output);
            Console.WriteLine(output);

            swData.Close();
            swOutput.Close();
        }

        static Tuple<int,string> sum(int a, string b) {
            return null;
        }
        private static void initWorkers()
        {
            workers = new Worker<Tuple<TravelingSpecimen,TravelingSpecimen>,TravelingSpecimen[]>[maxThreads];
            for (int i=0; i<maxThreads; i++) {
                Worker<Tuple<TravelingSpecimen,TravelingSpecimen>,TravelingSpecimen[]> w = 
                    new Worker<Tuple<TravelingSpecimen, TravelingSpecimen>, TravelingSpecimen[]>(combine);
                workers[i] = w;
                w.start();
            }
        }

        private static Tuple<decimal, decimal, decimal> createCandidates()
        {
            int keepCount = candidates.Count;
            List<TravelingSpecimen> cand = new List<TravelingSpecimen>();
            decimal[] oldFit = (from value in candidates select value.fit).ToArray();
            List<Tuple<TravelingSpecimen,TravelingSpecimen>> jobs = new List<Tuple<TravelingSpecimen, TravelingSpecimen>>();

            for (int i=0; i<candidates.Count-1; i++) {
                Tuple<TravelingSpecimen,TravelingSpecimen> job = 
                    new Tuple<TravelingSpecimen, TravelingSpecimen>(candidates[i],candidates[i+1]);
                jobs.Add(job);
            }

            if (workers==null) {
                foreach (Tuple<TravelingSpecimen,TravelingSpecimen> job in jobs) {
                    TravelingSpecimen[] results = combine(job);
                    cand.AddRange(results);
                }
            } else {
                int splitCount = workers.Length;
                int amount = jobs.Count/splitCount;
                int remainder = jobs.Count-(amount*splitCount);
                int[] amounts = new int[splitCount];

                for (int i=0; i<amounts.Length; i++) {
                    amounts[i] = amount;
                    if (remainder>0) {
                        amounts[i]++;
                        remainder--;
                    }
                } 

                List<List<Tuple<TravelingSpecimen,TravelingSpecimen>>> _jobs = new List<List<Tuple<TravelingSpecimen, TravelingSpecimen>>>();
               for (int i=0; i<amounts.Length; i++) {
                    List<Tuple<TravelingSpecimen,TravelingSpecimen>> __jobs = new List<Tuple<TravelingSpecimen, TravelingSpecimen>>();
                    for (int j=0; j<amounts[i]; j++) {
                        Tuple<TravelingSpecimen,TravelingSpecimen> k = jobs[0];
                        jobs.RemoveAt(0);
                        __jobs.Add(k);
                    }
                    
                    if (jobs.Count>0) {
                        __jobs.Add(jobs[0]);
                    } 
                    _jobs.Add(__jobs);
                }


                for (int i=0; i<workers.Length; i++) {
                    workers[i].addJobs(_jobs[i]);
                }   

                bool running;
                do {
                    running = false;
                    foreach (Worker<Tuple<TravelingSpecimen,TravelingSpecimen>,TravelingSpecimen[]> worker in workers) {
                        if (worker.JobRunning) {
                            running=true;
                            Thread.Sleep(500);
                            break;
                        }
                    }
                } while(running);

                foreach (Worker<Tuple<TravelingSpecimen,TravelingSpecimen>,TravelingSpecimen[]> worker in workers) {
                    while (worker.HasResults) {
                        List<TravelingSpecimen[]> results = worker.GetResults();
                        if (results!=null) {
                            foreach(TravelingSpecimen[] r in results) {
                                cand.AddRange(r);
                            }
                        } else {
                            throw new Exception("Error getting results");
                        }
                    }
                }
            }

            TravelingSpecimen[] keep = (from value in cand where !candidates.Contains(value) select value).ToArray();
            candidates.AddRange(keep);
            candidates.Sort();

            decimal[] fit = (from value in candidates select value.fit).ToArray();
            candidates = (List<TravelingSpecimen>)candidates.Take(keepCount).ToList();

            Tuple<decimal,decimal,decimal> result = new Tuple<decimal, decimal, decimal>(
                fit[0],fit[fit.Length-1],fit.Average()-oldFit.Average()
            );
            return result;
        }

        private static TravelingSpecimen[] combine(Tuple<TravelingSpecimen, TravelingSpecimen> job)
        {
            List<TravelingSpecimen> values = new List<TravelingSpecimen>();
            TravelingSpecimen[] children;
            children = job.Item1.combine(job.Item2);
            values.AddRange(children);

            children = job.Item2.combine(job.Item1);
            values.AddRange(children);
            foreach (TravelingSpecimen val in values) {
                val.fit = fitness(val);
            }

            return values.ToArray();
        }

        private static void initCandidates(List<TravelingSpecimen> _cand = null,List<Node[]> _nodes = null, int count = 512)
        {
            Random random = new Random();
            if (_cand!=null) {
                Program.candidates = _cand;
                return;
            }
            if (_nodes == null) {
                for (int i=0; i<count; i++) {
                    List<Node> cand = Program.nodes.ToArray().ToList();
                    List<Node> vals = new List<Node>();

                    int index = 0;
                    Node start = nodes[index];
                    vals.Add(start);
                    cand.RemoveAt(index);

                    while (cand.Count>0) {
                        index = random.Next(0,cand.Count);
                        vals.Add(cand[index]);
                        cand.RemoveAt(index);
                    }

                    vals.Add(start);
                    Program.candidates.Add(createCandidate(vals.ToArray()));
                }
            } else {
                foreach (Node[] value in _nodes) {
                    candidates.Add(createCandidate(value));
                }
            }
        }

        private static TravelingSpecimen createCandidate(Node[] value)
        {
            TravelingSpecimen specimen = new TravelingSpecimen(value);
            specimen.fit = fitness(specimen);
            return specimen;
        }

        private static void initNodes(List<Node> _nodes = null, int dimensions = 2, int count = 16)
        {
            Random random = new Random();

            if (_nodes != null) {
                Program.nodes = _nodes;
                return;
            }

            int min = 0;
            int max = 100;

            for (int i=min; i<max; i++) {
                List<int> pos = new List<int>();
                for (int j=0; j<dimensions; j++) {
                    pos.Add(random.Next(min,max));
                }
                Program.nodes.Add(new Node("Node" + i.ToString(), new Vector(pos)));
            }
        }

        private static List<Node> readNodes(string path) {
            List<Node> values = new List<Node>();
            int id = 0;

            using (StreamReader sr = new StreamReader(path)) {
                string line;
                while ((line=sr.ReadLine())!=null) {
                    string[] val = line.TrimEnd('\n').Split(',');
                    List<int> pos = new List<int>();
                    foreach (string v in val) {
                        pos.Add(int.Parse(v));
                    }
                    Node n = new Node(id,new Vector(pos));
                    id++;
                    values.Add(n);
                }
            }
            return values;
        }

        private static List<TravelingSpecimen> readCand(string path, int gen) {
            List<TravelingSpecimen> values = new List<TravelingSpecimen>();
            genCount = gen;
            using (StreamReader sr = new StreamReader(path)) {
                string line;
                sr.ReadLine(); // header
                while ((line=sr.ReadLine())!=null) {
                    string[] val = line.TrimEnd('\n').Split(',');
                    if (int.Parse(val[0])==gen) {
                        object id = val[1];
                        decimal fitness = decimal.Parse(val[2]);
                        int mutationCount = int.Parse(val[3]);
                        object parentIdA = val[4];
                        object parentIdB = val[5];
                        List<Node> list = new List<Node>();
                        for (int i=6; i<val.Length; i++) {
                            list.Add(new Node(val[i]));
                        }
                        values.Add(new TravelingSpecimen(list.ToArray(),id,parentIdA,parentIdB,mutationCount));
                    }
                }
            }
            return values;
        }

        static void Main(string[] args)
        {
            List<Node> nodes = null;
            List<TravelingSpecimen> cand = null;
            if (args.Length>0) {
                // Console.WriteLine(args.Length);
                // Console.WriteLine(string.Join(",",args));
                
                string nodeFile = string.Empty;
                string candFile = string.Empty;
                string candGen = string.Empty;
                for (int i=0; i<args.Length; i++) {
                    switch (args[i]) {
                        case "-n":
                            nodeFile = args[i+1];
                            break;
                        case "-c":
                            candFile = args[i+1];
                            candGen = args[i+2];
                            break;
                    }
                }
                if (nodeFile!=string.Empty) {
                    nodes = readNodes(nodeFile);
                }
                if (candFile!=string.Empty) {
                    cand = readCand(candFile,int.Parse(candGen));
                }
            }
            Program.beginTests(nodes,cand);            
        }
    }
}
