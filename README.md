# Shakespearean Monkeys
Build a distributed application that illustrates the Shakespearean Monkeys genetic algorithm.

Use ASP.NET, but to avoid needless complexities, we use the upper open source layer known as Carter, which features quite a few functional elements.
<br> We set up two standalone REST Carter servers: 
1. Monkeys, listening on HTTP port 8081; and 
2. Fitness, listening on HTTP port 8091. Here, the client will be simulated by testing scripts using curl and httprepl.

In the following diagrams, a bidirectional arrow indicates POST requests (3) and responses (4). Unidirectional arrows indicate POST requests with empty OK responses (1, 2, 5).
Also, all strings (target, genomes) exclusively consist of characters in the range 32..126 (i.e. printable ASCII).
![image](https://user-images.githubusercontent.com/49899381/98120078-78087c00-1ee8-11eb-8c24-5e379c47ba1c.png)

## JSON format
There are a few significant differences in the wire protocols (JSON):
1. POST /target sends a JSON object, formatted as:
```
{ “id”: int, “parallel”: Boolean, “target”: string }
```
Empty OK response.

2. POST /try sends a JSON object, formatted as:
```
{ “id”: int, “parallel”: Boolean, “monkeys”: number, “length”: number, “crossover”: number, “mutation”: number , “limit”: number }
```
length > 0 indicates the actual target length <br>
length = 0 indicates that this length must be dynamically discovered <br>
crossover and mutation indicate probabilities as percents, e.g. 87 indicates 0.87. <br>
limit, with default 1000, is the maximum number of generations that will be created by the evolution loop (this stops even if the top doesn’t achieve the perfect score 0)
Empty OK response. <br>

3. POST /assess includes an array (cf. C# list) of JSON strings (genomes), one for each monkey, such as:
```
{ “id”: int, “genomes”: [ “...”, “...”, ... “...” ] }
```
This request must be followed by a matching non-empty response (4). 


4. This POST response to (4) includes an array (cf. C# list) of numbers (fitness
scores), one for each sent genome, in the same order:
```
{ “id”: int, “scores”: [ number, number, ... number ] }
```
5. POST /top sends a JSON object, formatted as:
```
{ “id”: int, “score”: number, “genome”: string }
```
Genome is one of the top-ranking genomes for the current generation, and score its Fitness score. Empty OK response. <br> 
Steps (3, 4, 5) are repeated until a genome reaches the ideal score = 0. <br>
All POST bodies start now with an id, which is the port number of the client. With this convention, Fitness and Monkeys can support several concurrently running clients, each with its own port, targets, and requests (the default client port is 8101).
          
Each server runs, independently, in two distinct modes: (1) sequential mode, (2) parallel mode. If properly designed, the two execution modes share most of the code and only differ in their implementation of a critical loop. Attention at the parallel results (4), which must be returned in the same order as the received genomes.

## Bird’s eye view (Monkeys)
This genetic algorithm simulates the evolution of a population of “monkeys”, until they learn to reproduce specific target text. To properly model a real-life evolution, the simulated model must follow several rules. Here we give a bird’s eye view; further details are described in the following pages.
- The initial generation consist of randomly generated genomes, all of the given target length.
- If length = 0, Monkeys will have to discover the target lengths by trial and error.
- The model evolves via successive generations (this is the main loop).
- At each evolution step, the current generation (“parents”) creates an equally sized new generation (“children”).
- At the end of the evolution step, the new generation becomes current and the old generation is discarded.
- All genomes of the current (or initial generation) are sent to Fitness for evaluations.
- Each genome receives a score, indicating how close it is from the target. The score is the Hamming distance between the target and that genome. Thus, a lower score indicates a better fit.
- The breeding process which builds the new generation (this is the most critical inner loop) gives higher priority to genomes with lower scores (higher fitness); i.e. these are more likely to be selected as parents.
- The parent selection is with repetition. A low scoring (high fit) genome can be selected parent in many parent pairs.
- Although this will very rarely happen in a large population, the same genome can be accidentally selected for both parents. In this case, the two children are identical to this parent, regardless if a crossover occurs or not. This does not statistically affect the outcome and the simulation code is simpler and faster
- However, even the high scoring (low fitting) genomes still have a small but not null chance to contribute to the next generation.
- The evolution stops when one of the new genomes fully matches the target (i.e. has score = 0).

## Generic Algorithm
![image](https://user-images.githubusercontent.com/49899381/98120755-53f96a80-1ee9-11eb-878a-87424f2fa991.png)
<hr> 
The above pseudo-code uses the following additional input parameters:
- CrossoverProbability (e.g. 0.87): the probability that a pair of parents will cross over their genomes while generating a pair of children; otherwise, the children are identical to their parents
-  MutationProbability (e.g. 0.02): the probability that a newly generated child will suffer one random mutation

## Select Parent Generic Algorithm
![image](https://user-images.githubusercontent.com/49899381/98120939-9753d900-1ee9-11eb-9473-a4e9ee618c5c.png)
