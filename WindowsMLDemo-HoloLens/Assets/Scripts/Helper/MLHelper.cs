﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
#if WINDOWS_UWP
using Windows.AI.MachineLearning.Preview;
using Windows.Storage;
#endif
namespace WindowsMLDemos.Common.Helper
{
    public class MLHelper
    {
#if WINDOWS_UWP
    /// <summary>
    /// init a ML model
    /// </summary>
    /// <param name="file"></param>
    /// <param name="model"></param>
    /// <returns></returns>
    public async static Task CreateModelAsync(StorageFile file, IMachineLearningModel model)
        {
            var learningModel = await LearningModelPreview.LoadModelFromStorageFileAsync(file);
            model.LearningModel = learningModel;
        }
        /// <summary>
        /// evaluate a model
        /// </summary>
        /// <param name="input"></param>
        /// <param name="learningModel"></param>
        /// <returns></returns>
        public async static Task<IMachineLearningOutput> EvaluateAsync(IMachineLearningInput input, IMachineLearningModel learningModel)
        {
            var output = await learningModel.EvaluateAsync(input);
            return output;
        }
#endif

        /*
          Logistic sigmoid.
        */
        public static float Sigmoid(float x)
        {
            return (float)(1 / (1 + Mathf.Exp(-x)));
        }

        /*
          Computes the "softmax" function over an array.

          Based on code from https://github.com/nikolaypavlov/MLPNeuralNet/

          This is what softmax looks like in "pseudocode" (actually using Python
          and numpy):

              x -= np.max(x)
              exp_scores = np.exp(x)
              softmax = exp_scores / np.sum(exp_scores)

          First we shift the values of x so that the highest value in the array is 0.
          This ensures numerical stability with the exponents, so they don't blow up.
        */
        public static float[] Softmax(float[] z)
        {

            var z_exp = z.Select(Mathf.Exp);
            // [2.72, 7.39, 20.09, 54.6, 2.72, 7.39, 20.09]

            var sum_z_exp = z_exp.Sum();
            // 114.98

            var softmax = z_exp.Select(i => i / sum_z_exp);
            // [0.024, 0.064, 0.175, 0.475, 0.024, 0.064, 0.175]
            return softmax.ToArray();
        }

        public static Tuple<int, float> Argmax(float[] ary)
        {
            if (ary.Length <= 0)
            {
                return null;
            }
            var maxIndex = 0;
            var maxValue = ary[0];
            for (var i = 1; i < ary.Length; i++)
            {
                if (ary[i] > maxValue)
                {
                    maxValue = ary[i];
                    maxIndex = i;
                }
            }
            return new Tuple<int, float>(maxIndex, maxValue);
        }
    }
}
