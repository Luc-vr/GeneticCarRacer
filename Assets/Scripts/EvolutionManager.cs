using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace Assets.Scripts
{
    public class EvolutionManager : MonoBehaviour
    {
        [Header("Evolution settings")]
        public int populationSize = 10;
        public int numberOfGenerations = 10;
        public float topPerformersPercentage = 0.1f;
        public float mutationRate = 0.05f;
        public float mutationStrength = 0.2f;
        public float generationDuration = 10f;
        public float generationDurationIncrease = 0.5f;
        public float maxGenerationDuration = 45f;
        public bool reproduceSexually = true;
        public bool useElitism = true;

        [Header("Fitness weights")]
        public float averageSpeedWeight = 10f;
        public float distanceTraveledWeight = 1f;
        public float numberOfCheckpointsWeight = 200f;
        public float numberOfTrackLimitsWeight = -1000f;
        public float nextCheckpointDistanceWeight = -1f;

        [Header("Neural network settings")]
        public int numberOfHiddenLayers;
        public int numberOfNeuronsPerHiddenLayer;

        [Header("Game objects")]
        public GameObject carPrefab;
        public GameObject spawnPoint;

        private List<GameObject> cars = new();
        private FitnessCalculator fitnessCalculator;

        private int currentGeneration = 0;
        private float currentGenerationDuration = 0f;
        private float currentGenerationDurationTimer = 0f;

        private TextMeshProUGUI generationText;
        private TextMeshProUGUI previousFitnessText;
        private TextMeshProUGUI timeRemainingText;
        private TextMeshProUGUI carsRemainingText;

        private void Start()
        {
            currentGenerationDuration = generationDuration;
            fitnessCalculator = new FitnessCalculator(averageSpeedWeight, distanceTraveledWeight, numberOfCheckpointsWeight, numberOfTrackLimitsWeight, nextCheckpointDistanceWeight);
            generationText = GameObject.Find("GenerationText").GetComponent<TextMeshProUGUI>();
            previousFitnessText = GameObject.Find("PreviousFitnessText").GetComponent<TextMeshProUGUI>();
            timeRemainingText = GameObject.Find("TimeRemainingText").GetComponent<TextMeshProUGUI>();
            carsRemainingText = GameObject.Find("CarsRemainingText").GetComponent<TextMeshProUGUI>();
            StartFirstGeneration();
        }

        private void FixedUpdate()
        {
            currentGenerationDurationTimer += Time.deltaTime;
            if (currentGenerationDurationTimer >= currentGenerationDuration || GetActiveCars() <= 0)
            {
                currentGenerationDurationTimer = 0f;
                currentGenerationDuration += generationDurationIncrease;
                if (currentGenerationDuration > maxGenerationDuration)
                    currentGenerationDuration = maxGenerationDuration;
                currentGeneration++;
                Debug.Log($"Generation {currentGeneration} finished.");
                StopCurrentGeneration();
            }

            timeRemainingText.text = $"Time remaining: {currentGenerationDuration - currentGenerationDurationTimer:F1}";
            carsRemainingText.text = $"Cars remaining: {GetActiveCars()}";
        }

        private int GetActiveCars()
        {
            int activeCars = 0;
            foreach (GameObject carGO in cars)
            {
                if (carGO.activeSelf) activeCars++;
            }
            return activeCars;
        }

        private void StartFirstGeneration()
        {
            generationText.text = $"Generation: {currentGeneration + 1}";
            previousFitnessText.text = $"Previous fitness: -";

            // Create a population of cars
            for (int i = 0; i < populationSize; i++)
            {
                // Set up a neural network with random weights and biases
                NeuralNetwork neuralNetwork = new(18, 2, numberOfHiddenLayers, numberOfNeuronsPerHiddenLayer);
                neuralNetwork.FillRandomWeightsAndBiases();

                // Instantiate a new car
                GameObject carGO = Instantiate(carPrefab, spawnPoint.transform.position, spawnPoint.transform.rotation);
                NeuralCarController carController = carGO.GetComponent<NeuralCarController>();

                // Pass the neural network to the car controller
                carController.SetNeuralNetwork(neuralNetwork);

                cars.Add(carGO);
            }
        }

        private void StartNewGeneration(List<NeuralNetwork> parents, List<float> parentsFitnessScores)
        {
            generationText.text = $"Generation: {currentGeneration + 1}";

            // Create a new population of cars
            for (int i = 0; i < populationSize; i++)
            {
                NeuralNetwork neuralNetwork = new(18, 2, numberOfHiddenLayers, numberOfNeuronsPerHiddenLayer);

                if (useElitism && i < parents.Count)
                {
                    // Copy the neural network from the best performers of the previous generation
                    neuralNetwork.CopyWeightsAndBiases(parents[i]);
                } else
                {
                    NeuralNetwork parent = RouletteWheelSelector(parents, parentsFitnessScores);

                    if (reproduceSexually)
                    {
                        NeuralNetwork otherParent = RouletteWheelSelector(parents, parentsFitnessScores);
                        neuralNetwork.Crossover(parent, otherParent);
                    } else
                    {
                        neuralNetwork.CopyWeightsAndBiases(parent);
                    }

                    neuralNetwork.Mutate(mutationRate, mutationStrength);
                }

                // Instantiate a new car
                GameObject carGO = Instantiate(carPrefab, spawnPoint.transform.position, spawnPoint.transform.rotation);
                NeuralCarController carController = carGO.GetComponent<NeuralCarController>();
                carController.SetNeuralNetwork(neuralNetwork);

                cars.Add(carGO);
            }
        }

        private void StopCurrentGeneration()
        {
            List<NeuralCarController> carControllers = new();
            List<float> fitnessScores = new();

            // Calculate fitness for each car and store it
            foreach (GameObject carGO in cars)
            {
                // If not active, set active again
                if (!carGO.activeSelf)
                    carGO.SetActive(true);
                NeuralCarController carController = carGO.GetComponent<NeuralCarController>();
                float fitness = fitnessCalculator.CalculateFitness(carController);
                fitnessScores.Add(fitness);
                carControllers.Add(carController);
            }

            // Sort the cars by fitness in descending order
            var sortedCars = carControllers.Zip(fitnessScores, (car, fitness) => new { Car = car, Fitness = fitness })
                .OrderByDescending(x => x.Fitness)
                .ToList();

            // Select the top best-performing cars
            int numTopPerformers = Mathf.CeilToInt(populationSize * topPerformersPercentage);
            List<NeuralCarController> topPerformers = sortedCars.Take(numTopPerformers).Select(x => x.Car).ToList();

            // Destroy all game objects that are not yet destroyed
            foreach (GameObject carGO in cars)
            {
                if (carGO != null)
                    Destroy(carGO);
            }
            cars.Clear();

            // Extract the best networks
            List<NeuralNetwork> parents = topPerformers.Select(car => car.GetNeuralNetwork()).ToList();

            // Calculate fitness for all top performers
            List<float> parentsFitnessScores = new();

            foreach (NeuralCarController carController in topPerformers)
            {
                float fitness = fitnessCalculator.CalculateFitness(carController);
                parentsFitnessScores.Add(fitness);
            }

            if (currentGeneration > numberOfGenerations)
            {
                Debug.Log("Evolution finished.");
                return;
            }

            // Update the best fitness
            float bestFitnessThisGeneration = sortedCars[0].Fitness;

            // Update the previous fitness
            previousFitnessText.text = $"Previous fitness: {bestFitnessThisGeneration:F1}";

            // Call StartNewGeneration, passing the best networks
            StartNewGeneration(parents, parentsFitnessScores);
        }

        private NeuralNetwork RouletteWheelSelector(List<NeuralNetwork> parents, List<float> parentsFitnessScores)
        {
            float totalFitness = parentsFitnessScores.Sum();
            float randomFitness = UnityEngine.Random.Range(0f, totalFitness);
            float currentFitness = 0f;

            for (int i = 0; i < parents.Count; i++)
            {
                currentFitness += parentsFitnessScores[i];
                if (currentFitness >= randomFitness)
                {
                    return parents[i];
                }
            }

            return parents[0];
        }
    }
}
