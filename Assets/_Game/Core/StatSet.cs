using System.Collections.Generic;
using System.Linq;
namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit {}
}

public enum ModType
{
    Added,
    Increased,
    More
}

public record Modifier(string Stat, ModType Mod, float Value, object Source);
public class StatSet
{
  List<Modifier> stats =  new List<Modifier>();

  public float Get(string stat, float _base)
  {
      float added = stats.Where(x => x.Mod == ModType.Added&& x.Stat == stat).Select(x => x.Value).Sum();
      float incr = stats.Where(x => x.Mod == ModType.Increased&& x.Stat == stat).Select(x => x.Value).Sum();
      float more = 1f;
      foreach (var m in stats.Where(x=>x.Mod == ModType.More&& x.Stat == stat))
          more *=(1+ m.Value);
      return (_base + added) * (1 + incr) * more;
  }

  public void Add(Modifier mod)
  {
      stats.Add(mod);
  }

  public void RemoveBySource(object Source)
  {
      stats.RemoveAll(x => x.Source == Source);
  }
}
