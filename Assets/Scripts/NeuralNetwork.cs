using System;
using System.Linq;

namespace Assets.Scripts
{
    public class NeuralNetwork
    {
        public int numberOfInputs;
        public int numberOfOutputs;
        public int numberOfHiddenLayers;
        public int numberOfNeuronsPerHiddenLayer;
        public float[][] weights;
        public float[][] biases;

        public NeuralNetwork(int numberOfInputs, int numberOfOutputs, int numberOfHiddenLayers, int numberOfNeuronsPerHiddenLayer)
        {
            this.numberOfInputs = numberOfInputs;
            this.numberOfOutputs = numberOfOutputs;
            this.numberOfHiddenLayers = numberOfHiddenLayers;
            this.numberOfNeuronsPerHiddenLayer = numberOfNeuronsPerHiddenLayer;

            // Initialize weights and biases
            InitializeWeightsAndBiases();
        }

        private void InitializeWeightsAndBiases()
        {
            weights = new float[numberOfHiddenLayers + 1][];
            biases = new float[numberOfHiddenLayers + 1][];

            // Initialize input layer weights and biases
            weights[0] = new float[numberOfNeuronsPerHiddenLayer * numberOfInputs];
            biases[0] = new float[numberOfNeuronsPerHiddenLayer];

            // Initialize hidden layer weights and biases
            for (int i = 1; i < numberOfHiddenLayers; i++)
            {
                weights[i] = new float[numberOfNeuronsPerHiddenLayer * numberOfNeuronsPerHiddenLayer];
                biases[i] = new float[numberOfNeuronsPerHiddenLayer];
            }

            // Initialize output layer weights and biases
            weights[numberOfHiddenLayers] = new float[numberOfOutputs * numberOfNeuronsPerHiddenLayer];
            biases[numberOfHiddenLayers] = new float[numberOfOutputs];
        }

        public float[] Predict(float[] inputValues)
        {
            if (inputValues.Length != numberOfInputs)
                throw new ArgumentException("Input values length does not match the network's number of inputs.");

            // Forward propagation
            float[] currentLayer = inputValues;

            for (int layer = 0; layer <= numberOfHiddenLayers; layer++)
            {
                currentLayer = CalculateLayerOutput(currentLayer, weights[layer], biases[layer]);
            }

            return currentLayer;
        }

        private float[] CalculateLayerOutput(float[] input, float[] layerWeights, float[] layerBiases)
        {
            int outputSize = layerBiases.Length;
            float[] output = new float[outputSize];

            for (int i = 0; i < outputSize; i++)
            {
                output[i] = layerBiases[i] + DotProduct(input, layerWeights, i * input.Length);
            }

            // Apply activation function (e.g., ReLU)
            for (int i = 0; i < outputSize; i++)
            {
                //output[i] = Math.Max(0, output[i]); // ReLU activation
                output[i] = (float)Math.Tanh(output[i]);
            }

            return output;
        }

        private float DotProduct(float[] a, float[] b, int offsetB)
        {
            return a.Select((x, i) => x * b[i + offsetB]).Sum();
        }

        public void FillRandomWeightsAndBiases()
        {
            Random rand = new Random();

            for (int layer = 0; layer <= numberOfHiddenLayers; layer++)
            {
                for (int i = 0; i < weights[layer].Length; i++)
                {
                    weights[layer][i] = (float)(rand.NextDouble() * 2 - 1); // Random value between -1 and 1 for weights
                }

                for (int i = 0; i < biases[layer].Length; i++)
                {
                    biases[layer][i] = (float)(rand.NextDouble() * 2 - 1); // Random value between -1 and 1 for biases
                }
            }
        }


        public void Crossover(NeuralNetwork parent1, NeuralNetwork parent2)
        {
            Random rand = new();

            for (int layer = 0; layer <= numberOfHiddenLayers; layer++)
            {
                for (int i = 0; i < weights[layer].Length; i++)
                {
                    if (rand.NextDouble() < 0.5)
                    {
                        weights[layer][i] = parent1.weights[layer][i];
                    }
                    else
                    {
                        weights[layer][i] = parent2.weights[layer][i];
                    }
                }

                for (int i = 0; i < biases[layer].Length; i++)
                {
                    if (rand.NextDouble() < 0.5)
                    {
                        biases[layer][i] = parent1.biases[layer][i];
                    }
                    else
                    {
                        biases[layer][i] = parent2.biases[layer][i];
                    }
                }
            }
        }

        public void CopyWeightsAndBiases(NeuralNetwork parent)
        {
            for (int layer = 0; layer <= numberOfHiddenLayers; layer++)
            {
                for (int i = 0; i < weights[layer].Length; i++)
                {
                    weights[layer][i] = parent.weights[layer][i];
                }

                for (int i = 0; i < biases[layer].Length; i++)
                {
                    biases[layer][i] = parent.biases[layer][i];
                }
            }
        }

        public void Mutate(float MutationRate, float MutationStrength)
        {
            // Mutate weights
            for (int layer = 0; layer <= numberOfHiddenLayers; layer++)
            {
                for (int i = 0; i < weights[layer].Length; i++)
                {
                    if (UnityEngine.Random.Range(0f, 1f) < MutationRate)
                    {
                        weights[layer][i] += (float)UnityEngine.Random.Range(-1f, 1f) * MutationStrength;
                    }
                }

                for (int i = 0; i < biases[layer].Length; i++)
                {
                    if (UnityEngine.Random.Range(0f, 1f) < MutationRate)
                    {
                        biases[layer][i] += (float)UnityEngine.Random.Range(-1f, 1f) * MutationStrength;
                    }
                }
            }
        }
    }
}
