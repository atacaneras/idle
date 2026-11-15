using UnityEngine;

// Bina yukseltme kademeleri. Bir binadan kac adet alindigina gore 4 farkli gorunum:
//   Kademe 1:   1 -  25 adet   -> Art/<kat>/<bina_id>_lv1.png
//   Kademe 2:  26 -  50 adet   -> _lv2
//   Kademe 3:  51 - 100 adet   -> _lv3
//   Kademe 4: 101 - 200 adet   -> _lv4
// 200 adet ustune cikilamaz. Gorsel yoksa sahne placeholder cizer ve yine de kademeye gore sekil degistirir.
public static class BuildingTiers
{
    public const int Count = 4;
    public const int MaxCount = 200;

    static readonly int[] Starts = { 1, 26, 51, 101 };
    static readonly int[] Ends = { 25, 50, 100, 200 };

    public static int LevelFor(int count)
    {
        int level = 0;
        for (int i = 0; i < Starts.Length; i++)
            if (count >= Starts[i]) level = i;
        return level;
    }

    public static int Start(int level) { return Starts[Mathf.Clamp(level, 0, Count - 1)]; }
    public static int End(int level) { return Ends[Mathf.Clamp(level, 0, Count - 1)]; }

    // Sonraki kademenin baslangic adedi; son kademede -1.
    public static int NextCount(int level)
    {
        int next = level + 1;
        return next < Count ? Starts[next] : -1;
    }

    // "1-25 adet" gibi kisa etiket (lokalize edilebilir).
    public static string RangeLabel(int level)
    {
        return Loc.F("{0}-{1} adet", Start(level), End(level));
    }
}
