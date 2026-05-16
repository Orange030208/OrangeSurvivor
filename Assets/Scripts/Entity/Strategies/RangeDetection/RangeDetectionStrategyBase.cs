using System;

public abstract class RangeDetectionStrategyBase : IRangeDetectionStrategy
{
    protected readonly Entity owner;
    protected readonly PropertiesManager propertiesManager;

    protected RangeDetectionStrategyBase(
        Entity owner,
        PropertiesManager propertiesManager)
    {
        this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
        this.propertiesManager = propertiesManager ?? throw new ArgumentNullException(nameof(propertiesManager));
    }

    public abstract bool IsTargetInRange(Entity target);
}
