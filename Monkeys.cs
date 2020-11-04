namespace Monkeys {
    using Carter;
    using Microsoft.AspNetCore.Http;
    using Carter.ModelBinding;
    using Carter.Request;
    using Carter.Response;
    using System.Linq;
    using System.Collections.Generic;
    using System;
    using System.Text;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading;
    using System.Threading.Tasks;
    using static System.Console;
    
    public class HomeModule : CarterModule {
        static List<int> breedingWeighs;
        private List<string> currentGeneration;

        public HomeModule () {
            Post ("/try", async (req, res) => {
                var treq = await req.Bind<TryRequest> ();
                GeneticAlgorithm(treq);

                await Task.Delay (0);
                return;
            });
        }
        
        async Task<AssessResponse> PostFitnessAssess (AssessRequest areq) {
            var client = new HttpClient ();
            client.BaseAddress = new Uri ("http://localhost:8091/");
            client.DefaultRequestHeaders.Accept.Clear ();
            client.DefaultRequestHeaders.Accept.Add (new MediaTypeWithQualityHeaderValue ("application/json"));
            
            var hrm = await client.PostAsJsonAsync ("/assess", areq);
            hrm.EnsureSuccessStatusCode ();

            await Task.Delay (0);
            var res = await hrm.Content.ReadAsAsync <AssessResponse> ();
            WriteLine("res in PostFitnessAssess: " + res.id);
            foreach(int score in res.scores) {
                WriteLine("In PostFitnessAssess: " + score);
            }
            var ares = new AssessResponse {id = res.id, scores = res.scores};
            return ares;
        }
        
        async Task PostClientTop (TopRequest treq) {
            var client = new HttpClient();
            client.BaseAddress = new Uri("http://localhost:8101/");
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var hrm = await client.PostAsJsonAsync("/top", treq);
            hrm.EnsureSuccessStatusCode();
            
            await Task.Delay (0);
            return;
        }
        
        private Random _random = new Random (1);
        
        private double NextDouble () {
            lock (this) {
                return _random.NextDouble ();
            }
        }
        
        private int NextInt (int a, int b) {
            lock (this) {
                return _random.Next (a, b);
            }
        }

        int ProportionalRandom (int[] weights, int sum) {
            var val = NextDouble () * sum;
            
            for (var i = 0; i < weights.Length; i ++) {
                if (val < weights[i]) return i;
                
                val -= weights[i];
            }
            
            WriteLine ($"***** Unexpected ProportionalRandom Error");
            return 0;
        }

        async void GeneticAlgorithm (TryRequest treq) {
            WriteLine ($"..... GeneticAlgorithm {treq}");
            await Task.Delay (0);
            
            var id = treq.id;
            var monkeys = treq.monkeys;
            if (monkeys % 2 != 0) monkeys += 1;
            var length = treq.length;
            var crossover = treq.crossover / 100.0 ;
            var mutation = treq.mutation / 100.0;
            var limit = treq.limit;
            if (limit == 0) limit = 1000;
            var isParallel = treq.parallel;
            
            if (treq.length == 0) { //dynamic cal length
                var arr = new[] {""};
                var res = await PostFitnessAssess(new AssessRequest { id = id, genomes = arr.ToList(), });
                length = res.scores[0];
                WriteLine("dynamic detect length: " + length);
            }
    
            currentGeneration = new List<string>();
            currentGeneration = createRandoms(monkeys, length);
            foreach(string s in currentGeneration) {
                WriteLine("Random in the currentGeneration: " + s);
            }
            string currentBestGenome = currentGeneration[0];
            string targettxt = currentBestGenome;

            for (int loop = 0; loop < limit; loop ++) { // the evolutaion loop
                //Get the fitness values from the fitness server
                var res = await PostFitnessAssess(new AssessRequest { id = id, genomes = currentGeneration, });
                var fs = res.scores;
                foreach(int f in fs) {
                    WriteLine("fitness of currentGeneration: " + f);
                }

                var arr = new[] { currentBestGenome, targettxt };
                var res2 = await PostFitnessAssess(new AssessRequest {id = id, genomes = arr.ToList(), });
                var fs2 = res2.scores;
                WriteLine("currentbest and  best: "  + fs2[0] + " and " + fs2[1]);

                if (fs2[0] < fs2[1]) { //current best < best
                    WriteLine("==============================");
                    WriteLine("currentbest < best: "  + fs2[0] + " < " + fs2[1]);
                    targettxt = currentBestGenome;
                    await PostClientTop(new TopRequest { id = id, loop = loop, score = fs2[0], genome = targettxt, });
                    
                    if (fs2[0] == 0) break; //find the target
                }
                
                //create new generations
                currentGeneration = createNextGeneration(monkeys, mutation, crossover, fs, isParallel).ToList();
                currentBestGenome = getCurrentBest(fs);
            }

            WriteLine("targettxt: " + targettxt);
        }

        private List<string> createRandoms(int population, int length) {
            //random generata string population
            var randoms = new List<string>();
            for (int i = 0;i < population ; i++) {
                randoms.Add(createRandom(length));
            }
            return randoms;
        }

        private string createRandom(int length) {
            int asciiStart = 32; 
            int asciiEnd = 126; 
            StringBuilder sb = new StringBuilder();  
            for (int i = 0; i < length; i++) {
                sb.Append((char)(NextInt(asciiStart, asciiEnd + 1) % 255));
            }
            return sb.ToString();
        }

        private int getSumOfBreedingWeight(int maxFitness, List<int> fs) {
              breedingWeighs = fs.Select ( f => {
                    return (maxFitness - f  + 1);
                }) .ToList ();
              var SumOfbreedingWeigh = fs.Sum(f => (maxFitness - f + 1));
              return SumOfbreedingWeigh;
        }


        private string selectHgihFitParent(int sumOfBW) {
            var i = ProportionalRandom(breedingWeighs.ToArray(), sumOfBW);
     
            return currentGeneration[i];
            
        
            //throw new InvalidOperationException("Not to be.");
        }

        private string randomChangeOneCha(string c) {
            int asciiStart = 32; 
            int asciiEnd = 126; 
            var sb = new StringBuilder(c);
            sb[NextInt(0, c.Length)] = (char)(NextInt(asciiStart, asciiEnd + 1) % 255);
            return sb.ToString();
        }

        private string[] createChildren(double mutation, double crossover, string p1, string p2) {
            string c1, c2;
            if (NextDouble() < crossover) {
                int crossoverVal = NextInt(1, p1.Length);
                c1 =  p1.Substring(0, crossoverVal) + p2.Substring(crossoverVal);
                c2 =  p2.Substring(0, crossoverVal) + p1.Substring(crossoverVal);
            } else {
                c1 = p1;
                c2 = p2;
            }

            //Mutate the children
            if (NextDouble() <  mutation) {
                c1 = randomChangeOneCha(c1);
            }
            
            if (NextDouble() < mutation) {
                c2 = randomChangeOneCha(c2);
            }

            //Return the children
            return new[] { c1, c2 };
        }

        private string[] createNextGeneration(int population, double mutation, double crossover, List<int> fs, bool isParallel) {
            var maxFitness = fs.Max();
            var sumOfBW = getSumOfBreedingWeight(maxFitness, fs);
            WriteLine("maxFitness: " + maxFitness + "  sum of BW: " + sumOfBW);
            //create new generations
            
            if (isParallel) {
                return (from i in ParallelEnumerable.Range(0, population / 2)
                        from child in createChildren(mutation, crossover, 
                            selectHgihFitParent(sumOfBW), selectHgihFitParent(sumOfBW))
                        select child).ToArray();
            } else {
                return (from i in Enumerable.Range(0, population / 2)
                    from child in createChildren(mutation, crossover, 
                        selectHgihFitParent(sumOfBW), selectHgihFitParent(sumOfBW))
                    select child).ToArray();
            }
        }

        private string getCurrentBest(List<int> fs) {
            var index = fs.IndexOf(fs.Min());
            return currentGeneration[index];
        }
    }
    
    // public class TargetRequest {
        // public int id { get; set; }
        // public bool parallel { get; set; }
        // public string target { get; set; }
        // public override string ToString () {
            // return $"{{{id}, {parallel}, \"{target}\"}}";
        // }  
    // }    

    public class TryRequest {
        public int id { get; set; }
        public bool parallel { get; set; }
        public int monkeys { get; set; }
        public int length { get; set; }
        public int crossover { get; set; }
        public int mutation { get; set; }
        public int limit { get; set; }
        public override string ToString () {
            return $"{{{id}, {parallel}, {monkeys}, {length}, {crossover}, {mutation}, {limit}}}";
        }
    }
    
    public class TopRequest {
        public int id { get; set; }
        public int loop { get; set; }
        public int score { get; set; }
        public string genome { get; set; }
        public override string ToString () {
            return $"{{{id}, {loop}, {score}, {genome}}}";
        }  
    }    
    
    public class AssessRequest {
        public int id { get; set; }
        public List<string> genomes { get; set; }
        public override string ToString () {
            return $"{{{id}, #{genomes.Count}}}";
        }  
    }
    
    public class AssessResponse {
        public int id { get; set; }
        public List<int> scores { get; set; }
        public override string ToString () {
            return $"{{{id}, #{scores.Count}}}";
        }  
    }   
}

namespace Monkeys {
    using Carter;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.Extensions.DependencyInjection;

    public class Startup {
        public void ConfigureServices (IServiceCollection services) {
            services.AddCarter ();
        }

        public void Configure (IApplicationBuilder app) {
            app.UseRouting ();
            app.UseEndpoints( builder => builder.MapCarter ());
        }
    }
}

namespace Monkeys {
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;

    public class Program {
        public static void Main (string[] args) {
//          var host = Host.CreateDefaultBuilder (args)
//              .ConfigureWebHostDefaults (webBuilder => webBuilder.UseStartup<Startup>())

            var urls = new[] {"http://localhost:8081"};
            
            var host = Host.CreateDefaultBuilder (args)
            
                .ConfigureLogging (logging => {
                    logging
                        .ClearProviders ()
                        .AddConsole ()
                        .AddFilter (level => level >= LogLevel.Warning);
                })
                
                .ConfigureWebHostDefaults (webBuilder => {
                    webBuilder.UseStartup<Startup> ();
                    webBuilder.UseUrls (urls);  // !!!
                })
                
                .Build ();
            
            System.Console.WriteLine ($"..... starting on {string.Join (", ", urls)}");            
            host.Run ();
        }
    }
}

