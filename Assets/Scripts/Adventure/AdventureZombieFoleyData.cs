using System.Collections.Generic;
namespace PvZReplica.Adventure {
public sealed class AdventureZombieFoleyDefinition {
public float PitchRange;public string[] Samples;
public AdventureZombieFoleyDefinition(float pitch,string[] samples){PitchRange=pitch;Samples=samples;}
}
public static class AdventureZombieFoleyData {
public static readonly Dictionary<int,AdventureZombieFoleyDefinition> Definitions=new Dictionary<int,AdventureZombieFoleyDefinition> {
{ 3,new AdventureZombieFoleyDefinition(10f,new[]{"throw","throw","throw","throw2"}) },
{ 18,new AdventureZombieFoleyDefinition(0f,new[]{"frozen"}) },
{ 27,new AdventureZombieFoleyDefinition(10f,new[]{"snow_pea_sparkles"}) },
{ 20,new AdventureZombieFoleyDefinition(-3f,new[]{"bowlingimpact"}) },
{ 5,new AdventureZombieFoleyDefinition(0.0f,new[]{"chomp","chomp2"}) },
{ 6,new AdventureZombieFoleyDefinition(4.0f,new[]{"chompsoft"}) },
{ 68,new AdventureZombieFoleyDefinition(0.0f,new[]{"floop"}) },
{ 72,new AdventureZombieFoleyDefinition(1.0f,new[]{"yuck","yuck","yuck2"}) },
{ 1,new AdventureZombieFoleyDefinition(10.0f,new[]{"splat","splat2","splat3"}) },
{ 25,new AdventureZombieFoleyDefinition(10.0f,new[]{"limbs_pop"}) },
{ 28,new AdventureZombieFoleyDefinition(10.0f,new[]{"zombie_falling_1","zombie_falling_2"}) },
{ 32,new AdventureZombieFoleyDefinition(10.0f,new[]{"kernelpult","kernelpult2"}) },
{ 38,new AdventureZombieFoleyDefinition(5.0f,new[]{"ignite","ignite","ignite","ignite2"}) },
{ 40,new AdventureZombieFoleyDefinition(2.0f,new[]{"gargantuar_thump"}) },
{ 43,new AdventureZombieFoleyDefinition(2.0f,new[]{"butter"}) },
{ 46,new AdventureZombieFoleyDefinition(10.0f,new[]{"shieldhit","shieldhit2"}) },
{ 58,new AdventureZombieFoleyDefinition(5.0f,new[]{"plastichit","plastichit2"}) },
{ 62,new AdventureZombieFoleyDefinition(-5.0f,new[]{"melonimpact","melonimpact2"}) },
{ 67,new AdventureZombieFoleyDefinition(-2.0f,new[]{"newspaper_rip"}) },
};
}}
