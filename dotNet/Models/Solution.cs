using System;
namespace dotNet.Models 
{
  public class Solution 
  {
    public int steps { get; set; }
    public String solution { get; set; }

    public Solution(int steps, string solution)
    {
      this.steps = steps;
      this.solution = solution;
    }
  }
}