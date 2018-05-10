namespace Word2VecScenario
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using Word2Vec.Net;

    class Program
    {
        static string path = @"Word2VectorOutputFile.bin";
        static Distance distance = null;
        static WordAnalogy wordAnalogy = null;

        static void Main(string[] args)
        {
            // -train <file> Use text data from <file> to train the model
            string train = "Corpus.txt";

            // -output <file> Use <file> to save the resulting word vectors / word clusters
            string output = "Vectors.bin";

            // -save-vocab <file> The vocabulary will be saved to <file>
            string savevocab = "";

            // -read-vocab <file> The vocabulary will be read from <file>, not constructed from the training data
            string readvocab = "";

            // -size <int> Set size of word vectors; default is 100
            int size = 100;

            // -debug <int> Set the debug mode (default = 2 = more info during training)
            int debug = 1;

            // -binary <int> Save the resulting vectors in binary moded; default is 0 (off)
            int binary = 1;

            // -cbow <int> Use the continuous bag of words model; default is 1 (use 0 for skip-gram model)
            int cbow = 1;

            // -alpha <float> Set the starting learning rate; default is 0.025 for skip-gram and 0.05 for CBOW
            float alpha = 0.05f;

            // -sample <float> Set threshold for occurrence of words. Those that appear with higher frequency in the training data
            float sample = 1e-4f;

            // -hs <int> Use Hierarchical Softmax; default is 0 (not used)
            int hs = 0;

            // -negative <int> Number of negative examples; default is 5, common values are 3 - 10 (0 = not used)
            int negative = 5;

            // -threads <int> Use <int> threads (default 12)
            int threads = 12;

            // -iter <int> Run more training iterations (default 5)
            long iter = 15;

            // -min-count <int> This will discard words that appear less than <int> times; default is 5
            int mincount = 5;

            // -classes <int> Output word classes rather than word vectors; default number of classes is 0 (vectors are written)
            long classes = 0;

            // -window <int> Set max skip length between words; default is 5
            int window = 12;

            Word2Vec word2Vec = new Word2Vec(train, output, savevocab, readvocab, size, debug, binary, cbow, alpha, sample, hs, negative, threads, iter, mincount, classes, window);
            
            var totalTime = Stopwatch.StartNew();
            var highRes = Stopwatch.IsHighResolution;

            word2Vec.TrainModel();

            totalTime.Stop();

            var trainingTime = totalTime.ElapsedMilliseconds;
            Console.WriteLine("Training took {0}ms", trainingTime);

            path = @"Vectors.bin";
            distance = new Distance(path);
            wordAnalogy = new WordAnalogy(path);

            string[] wordList = new string[] {"paris france madrid" };

            var searchTime = Stopwatch.StartNew();

            foreach (string word in wordList)
            {
                distance.Search(word);
                wordAnalogy.Search(word);
            }

            searchTime.Stop();
            var firstSearchTime = searchTime.ElapsedMilliseconds;
            Console.WriteLine("Search took {0}ms", firstSearchTime);

            int outerN = 5;

            for (int outer = 0; outer < outerN; outer++)
            {
                foreach (string word in wordList)
                {
                    int N = 11;
                    var minSearchTime = long.MaxValue;
                    var maxSearchTime = long.MinValue;
                    long[] searchTimes = new long[N];

                    Console.WriteLine($"Batch {outer}, searching {word}: running {N} searches");

                    for (int inner = 0; inner < N; inner++)
                    {
                        searchTime.Restart();
                        distance.Search(word);
                        BestWord[] result = wordAnalogy.Search(word);
                        searchTime.Stop();

                        /*foreach (var bestWord in result)
                        {
                            Console.WriteLine("{0}\t\t{1}", bestWord.Word, bestWord.Distance);
                        }*/

                        long interval = highRes ? searchTime.ElapsedTicks : searchTime.ElapsedMilliseconds;
                        searchTimes[inner] = interval;

                        if (interval < minSearchTime)
                        {
                            minSearchTime = interval;
                        }
                        if (interval > maxSearchTime)
                        {
                            maxSearchTime = interval;
                        }
                    }

                    if (highRes)
                    {
                        double averageSearch = 1000 * ((double)searchTimes.Sum() / N / Stopwatch.Frequency);
                        double medianSearch = 1000 * ((double)searchTimes.OrderBy(t => t).ElementAt(N / 2) / Stopwatch.Frequency);
                        Console.WriteLine("Steadystate min search time: {0:F2}ms", (1000 * minSearchTime) / Stopwatch.Frequency);
                        Console.WriteLine("Steadystate max search time: {0:F2}ms", (1000 * maxSearchTime) / Stopwatch.Frequency);
                        Console.WriteLine("Steadystate average search time: {0:F2}ms", averageSearch);
                        Console.WriteLine("Steadystate median search time: {0:F2}ms", medianSearch);
                    }
                    else
                    {
                        long averageSearch = searchTimes.Sum() / N;
                        long medianSearch = searchTimes.OrderBy(t => t).ElementAt(N / 2);
                        Console.WriteLine("Steadystate min search time: {0}ms", minSearchTime);
                        Console.WriteLine("Steadystate max search time: {0}ms", maxSearchTime);
                        Console.WriteLine("Steadystate average search time: {0}ms", (int)averageSearch);
                        Console.WriteLine("Steadystate median search time: {0}ms", (int)medianSearch);
                    }

                    Console.WriteLine("");
                }
            }
        }

    }

}
