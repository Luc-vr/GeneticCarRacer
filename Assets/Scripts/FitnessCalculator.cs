using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Scripts
{
    public class FitnessCalculator
    {
        public float averageSpeedWeight;
        public float distanceTraveledWeight;
        public float checkpointsPassedWeight;
        public float trackLimitsWeight;
        public float nextCheckpointDistanceWeight;

        public FitnessCalculator(float averageSpeedWeight, float distanceTraveledWeight, float checkpointsPassedWeight, float trackLimitsWeight, float nextCheckpointDistanceWeight)
        {
            this.averageSpeedWeight = averageSpeedWeight;
            this.distanceTraveledWeight = distanceTraveledWeight;
            this.checkpointsPassedWeight = checkpointsPassedWeight;
            this.trackLimitsWeight = trackLimitsWeight;
            this.nextCheckpointDistanceWeight = nextCheckpointDistanceWeight;
        }

        public float CalculateFitness(NeuralCarController car)
        {
            return 
                (car.GetAverageSpeed() * averageSpeedWeight) + 
                (car.totalDistanceTraveled * distanceTraveledWeight) + 
                (car.numberOfCheckpointsPassed * checkpointsPassedWeight) + 
                (car.numberOfTrackLimits * trackLimitsWeight) +
                (car.nextCheckpointDistance * nextCheckpointDistanceWeight);
        }
    }
}
