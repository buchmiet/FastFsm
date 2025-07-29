namespace Generator.ModernGeneration.Features
{
    /// <summary>
    /// Moduł może dodawać pola do klasy.
    /// </summary>
    public interface IEmitFields : IFeatureModule
    {
        void EmitFields(Context.GenerationContext ctx);
    }
}
