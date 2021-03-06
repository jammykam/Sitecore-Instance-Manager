﻿namespace SIM.Pipelines
{
  #region

  using System.Collections.Generic;
  using SIM.Pipelines.Processors;
  using Sitecore.Diagnostics.Base;
  using JetBrains.Annotations;
  using SIM.Extensions;

  #endregion

  public class Step
  {
    #region Fields

    [CanBeNull]
    public readonly string ArgsName;

    [NotNull]
    public readonly List<Processor> Processors;

    #endregion

    #region Constructors

    public Step([NotNull] List<Processor> processors, [CanBeNull] string argsName)
    {
      Assert.ArgumentNotNull(processors, nameof(processors));

      this.Processors = processors;
      this.ArgsName = argsName;
    }

    #endregion

    #region Public Methods

    [NotNull]
    public static List<Step> CreateSteps([NotNull] List<StepDefinition> stepDefinitions, [NotNull] ProcessorArgs args, [CanBeNull] IPipelineController controller = null)
    {
      Assert.ArgumentNotNull(stepDefinitions, nameof(stepDefinitions));
      Assert.ArgumentNotNull(args, nameof(args));

      return new List<Step>(CreateStepsPrivate(stepDefinitions, args, controller));
    }

    #endregion

    #region Methods

    [NotNull]
    private static IEnumerable<Step> CreateStepsPrivate([NotNull] IEnumerable<StepDefinition> steps, [NotNull] ProcessorArgs args, [CanBeNull] IPipelineController controller = null)
    {
      Assert.ArgumentNotNull(steps, nameof(steps));
      Assert.ArgumentNotNull(args, nameof(args));

      foreach (StepDefinition stepDefinition in steps)
      {
        var argsName = stepDefinition.ArgsName.EmptyToNull();
        Step step = new Step(ProcessorManager.CreateProcessors(stepDefinition.ProcessorDefinitions, args, controller), argsName);
        Assert.IsNotNull(step, "Can't instantiate step");
        yield return step;
      }
    }

    #endregion
  }
}