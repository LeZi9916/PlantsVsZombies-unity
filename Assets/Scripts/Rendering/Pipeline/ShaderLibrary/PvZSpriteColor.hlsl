#ifndef PVZ_SPRITE_COLOR_INCLUDED
#define PVZ_SPRITE_COLOR_INCLUDED

// PC FilterEffectDoLumSat (451EC0) preserves hue and scales HSL saturation and lightness.
float3 ApplyImitaterFilter(float3 source, float filter)
{
    float maximum = max(source.r, max(source.g, source.b));
    float minimum = min(source.r, min(source.g, source.b));
    float lightness = (maximum + minimum) * 0.5;
    float chroma = maximum - minimum;
    float saturation = chroma / max(1e-6, 1.0 - abs(2.0 * lightness - 1.0));
    float3 hue = (source - minimum) / max(1e-6, chroma);

    bool lessWashed = filter > 2.5;
    lightness = saturate(lightness * (lessWashed ? 1.2 : 1.8));
    saturation *= lessWashed ? 0.3 : 0.2;
    chroma = (1.0 - abs(2.0 * lightness - 1.0)) * saturation;
    return floor(saturate(lightness + (hue - 0.5) * chroma) * 255.0) / 255.0;
}

#endif
