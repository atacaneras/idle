using System;
using System.Collections.Generic;

[Serializable]
public class GameState
{
    public int version = 1;          // kayit formati degisirse artir ve migrate et
    public long lastSaveUnix;
    public string ageId;
    public double knowledge;
    public List<NamedDouble> resources = new List<NamedDouble>();
    public List<NamedInt> buildings = new List<NamedInt>();
}

[Serializable] public struct NamedDouble { public string id; public double value; }
[Serializable] public struct NamedInt { public string id; public int value; }
