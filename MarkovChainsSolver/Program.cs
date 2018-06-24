﻿using MarkovChains;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarkovChains
{
    class Program
    {
        static void Main(string[] args)
        {
            List<List<decimal>> mchain = new List<List<decimal>>
            {
                new List<decimal>{0.65m, 0.15m, 0.1m},
                new List<decimal>{0.25m, 0.65m, 0.4m},
                new List<decimal>{0.1m,  0.2m,  0.5m},
            };

            MarkovChain markovChain = new MarkovChain(mchain);
            markovChain.SolveSteadyStates();

            Console.ReadLine();
        }
    }

    class MarkovChain
    {

        private List<List<decimal>> markovChain;
        private List<string> names;
        private List<SteadyStateEquation> steadyStateEquations;
        private List<SteadyStateValue> solvedSteadyStateValues;

        public MarkovChain(List<List<decimal>> markovChain)
        {
            this.markovChain = markovChain;
            GenerateEquations();
            solvedSteadyStateValues = new List<SteadyStateValue>();
        }

        private void GenerateEquations()
        {
            steadyStateEquations = new List<SteadyStateEquation>();
            for (int i = 0; i < markovChain.Count; i++)
                steadyStateEquations.Add(new SteadyStateEquation(markovChain[i], new SteadyStateValue((i + 1).ToString(), 1)));
        }

        public void Setnames(List<string> names)
        {
            if (names.Count == this.names.Count)
                this.names = names;
            else
                throw new Exception("Length of List 'names' does not match dimensions of markov chain");
        }

        public void SolveSteadyStates()
        {
            steadyStateEquations.ForEach(Console.WriteLine);
            steadyStateEquations.ForEach(s => s.solve());
            steadyStateEquations.ForEach(Console.WriteLine);
            steadyStateEquations[0].substituteEquation(steadyStateEquations[1]);
            steadyStateEquations[1].substituteEquation(steadyStateEquations[2]);
            steadyStateEquations[2].substituteEquation(steadyStateEquations[0]);
            steadyStateEquations.ForEach(Console.WriteLine);
            //what is the next step
            //solve all in terms of p1...
            //focus on the steps require to get it done first...
            //focus on automating it later...
            steadyStateEquations[2].substituteEquation(steadyStateEquations[1]);//TODO: figuring out when to do this must be automated
            steadyStateEquations.ForEach(Console.WriteLine);
            SubstituteIntoStar();
            //now solve into one
        }

        private void SubstituteIntoOne() //NOTE: This method assumes that all equations are solved in terms of π1
        {
            //UNDONE: step 1: validate readiness 

            //step 2: sub into (*)
            decimal sum = 1;
            foreach (SteadyStateEquation s in steadyStateEquations)
            {
                if (s.SteadyStateValues.Count != 1 /*|| !s.SteadyStateValues[0].K.Equals("1")*/) //NOTE: second arg not need unless I decide to use a different technique
                    throw new Exception("Cannot substitute into (*): equations are not subsequently solved yet");

                if (s.SteadyStateValues[0].K.Equals("1"))
                {
                    sum += s.SteadyStateValues[0].Value;
                }
            }
            Console.WriteLine(sum);

        }

        private class SteadyStateEquation
        {
            public List<SteadyStateValue> SteadyStateValues { get; set; }
            public SteadyStateValue Equivalent { get; set; }

            public SteadyStateEquation(List<decimal> values, SteadyStateValue equivalent)
            {
                SteadyStateValues = new List<SteadyStateValue>();

                for (int i = 0; i < values.Count; i++)
                    SteadyStateValues.Add(new SteadyStateValue((i + 1).ToString(), values[i]));
                Equivalent = equivalent;
            }

            public override string ToString()
            {
                string equation = "";

                for (int j = 0; j < SteadyStateValues.Count - 1; j++)
                    equation += $"{SteadyStateValues[j].ToString()} + ";
                equation += $"{SteadyStateValues.Last().ToString()} = {Equivalent.ToString()}";

                return equation;
            }

            #region substitution_steps
            public void substituteEquation(params SteadyStateEquation[] subEquations)
            {
                for (int i = SteadyStateValues.Count - 1; i >= 0; i--)
                    foreach (SteadyStateEquation subEquation in subEquations)
                        if (SteadyStateValues[i].K == (subEquation.Equivalent.K))
                            SubstituteValue(i, subEquation);
                solve();
            }

            private void SubstituteValue(int oldSteadyStateValueIndex, SteadyStateEquation newSteadyStateValues)
            {
                decimal p = SteadyStateValues[oldSteadyStateValueIndex].Value;
                foreach (SteadyStateValue newSteadyStateValue in newSteadyStateValues.SteadyStateValues)
                    SteadyStateValues.Add(new SteadyStateValue(newSteadyStateValue.K, newSteadyStateValue.Value * p));
                SteadyStateValues.RemoveAt(oldSteadyStateValueIndex);
            }

            public void solve()
            {
                Consolidate();
                //step 1: take relevant value out
                for (int i = SteadyStateValues.Count - 1; i >= 0; i--)
                    if (SteadyStateValues[i].K.Equals(Equivalent.K))
                    {
                        Equivalent.Value = 1 - SteadyStateValues[i].Value;
                        SteadyStateValues.RemoveAt(i);
                        break;
                    } //NOTE: not entirely necessary unless showing working is required

                //step 2: adjust such that the equiv = 1
                SteadyStateValues.ForEach(s => s.Value /= Equivalent.Value);
                Equivalent.Value = 1;
            }

            public void Consolidate() //there is probably a better way to do this
            {
                List<int> removalIndices = new List<int>();

                for (int i = SteadyStateValues.Count - 1; i >= 0; i--)
                    for (int j = SteadyStateValues.Count - 1; j >= 0; j--)
                        if (i != j && SteadyStateValues[i].K.Equals(SteadyStateValues[j].K) && !removalIndices.Contains(j))
                        {
                            decimal p = SteadyStateValues[i].Value;
                            removalIndices.Add(i);
                            SteadyStateValues[j].Value += p;
                        }

                removalIndices.ForEach(i => SteadyStateValues.RemoveAt(i));
            }
            #endregion substitution_steps

        }

        private class SteadyStateValue
        {
            public string K { get; set; }
            public decimal Value { get; set; }

            public SteadyStateValue(string k, decimal value)
            {
                K = k;
                Value = value;
            }

            public override string ToString()
            {
                if (Value == 1)
                    return $"π{K}";
                else
                    return $"{Math.Round(Value, 4)}π{K}";
            }
        }


    }
}

#region dump
#endregion dump