namespace Generator.ModernGeneration.Features
{
    /// <summary>
    /// Moduł może dodawać using statements.
    /// </summary>
    public interface IEmitUsings : IFeatureModule
    {
        void EmitUsings(Context.GenerationContext ctx);
    }
}
