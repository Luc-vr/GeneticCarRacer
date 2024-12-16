# Genetic car racer

This Unity project demonstrates the application of genetic algorithms, a technique that mimics natural evolution to find solutions for complex problems. The project contains a demo in which a car is trained to drive around a circuit, showcasing how genetic algorithms can be used in combination with artificial intelligence.

![genetic-car-racer-demo](https://github.com/user-attachments/assets/74f7982e-7b3e-4bbe-9e2f-539acd6400d1)

Key Features:

- Implementation of genetic algorithms in several steps: Initialization, Evaluation, Selection, and Reproduction.
- Implementation of roulette wheel selection and elitism
- Use of genetic algorithms to train a neural network.
- Application of the neural network to control a car in a 2D Unity environment
- Configurable settings for the genetic algorithm

## Prerequisites
Beforehand make sure you have Unity 2021.3 and Unity Hub installed.

## Getting started

1. Clone the project
2. Open Unity hub -> Add -> Add project from disk and select the correct directory.
3. Open the Unity project (this may take a few minutes)
4. In the Unity editor, go to _Assets/Scenes_ and select one of the 3 circuits.
5. Click on the play button at the top of the screen and the training should start!

(Tip: change the aspect from Free Aspect to 16:9 Aspect in the game tab)

## Configuring the genetic algorithm

You can experiment with different settings for the genetic algorithm. To do so, select the `EvolutionManager` Game object. All of the parameters are now listed in the inspector, under the `Evolution Manager (Script)` tab. Below is a brief description of each parameter listed under evolution settings:

| Parameter                    | Description                                                                                                                       |
|------------------------------|-----------------------------------------------------------------------------------------------------------------------------------|
| Population size              | The number of cars that make up each generation                                                                                   |
| Number of generations        | The number of generations after which the training will stop                                                                      |
| Top performers percentage    | The percentage of elites (0.1 = 10%)                                                                                              |
| Mutation rate                | The probability that a mutation will happen on a gen (0.3 = 30%)                                                                  |
| Mutation strength            | The maximum amount a mutation can alter a gen (0.3 means that a mutation will add a random value between -0.3 and 0.3 to the gen) |
| Generation duration          | The initial training duration in seconds                                                                                          |
| Generation duration increase | The amount of seconds that gets added to the training duration for each generation in seconds                                     |
| Max generation duration      | The maximum amount of seconds a generation can train for                                                                          |
| Reproduce sexually           | Whether or not the reproduction is sexually. If false, reproduction will be asexual                                               |
| Use elitism                  | Whether or not elitism should be used. If false, top performers percentage will not be used                                         |

Other settings that are listed here as well are:
**Fitness weights**: the weights used to calculate the fitness for each car
**Neural network settings**: the size of the neural network which is being trained

If you want to do any additional configuration, you will need to edit the scripts found in the _Assets/Scripts_ directory.
