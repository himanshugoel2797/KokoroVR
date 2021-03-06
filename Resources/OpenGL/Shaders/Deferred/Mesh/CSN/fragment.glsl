// Interpolated values from the vertex shaders
in vec2 UV;
in vec3 normal;
in vec3 pos;
flat in int drawID;

// Ouput data
layout(location = 0) out vec4 Color;
layout(location = 1) out vec4 Normal;
layout(location = 2) out vec4 Specular;

struct obj_t {
    uvec2 c;
    uvec2 s;
    uvec2 n;
};

layout(std430, binding = 0) buffer Objects_t {
    obj_t v[];
} Object;

//pack into a single 32-bit float, for the gbuffer
float encode (vec3 n)
{
    return uintBitsToFloat(packSnorm4x8(vec4(n, 1)));
}

vec3 CalculateSurfaceGradient(vec3 n, vec3 dpdx, vec3 dpdy, float dhdx, float dhdy)
{
    vec3 r1 = cross(dpdy, n);
    vec3 r2 = cross(n, dpdx);
 
    return (r1 * dhdx + r2 * dhdy) / dot(dpdx, r1);
}
 
// Move the normal away from the surface normal in the opposite surface gradient direction
vec3 PerturbNormal(vec3 n, vec3 dpdx, vec3 dpdy, float dhdx, float dhdy)
{
    return normalize(normal - CalculateSurfaceGradient(normal, dpdx, dpdy, dhdx, dhdy));
}

float ApplyChainRule(float dhdu, float dhdv, float dud_, float dvd_)
{
    return dhdu * dud_ + dhdv * dvd_;
}

// Calculate the surface normal using the uv-space gradient (dhdu, dhdv)
vec3 CalculateSurfaceNormal(vec3 position, vec3 normal, vec2 gradient)
{
    vec3 dpdx = dFdx(position);
    vec3 dpdy = dFdy(position);
 
    float dhdx = ApplyChainRule(gradient.x, gradient.y, dFdx(UV.x), dFdx(UV.y));
    float dhdy = ApplyChainRule(gradient.x, gradient.y, dFdy(UV.x), dFdy(UV.y));
 
    return PerturbNormal(normal, dpdx, dpdy, dhdx, dhdy);
}

void main(){
    //Read object properties
    vec4 _colorMap = texture(sampler2D(Object.v[drawID].c), UV);
    vec4 _specularMap = texture(sampler2D(Object.v[drawID].s), UV);
    vec4 _normalMap = texture(sampler2D(Object.v[drawID].n), UV);

    float n_enc = encode(CalculateSurfaceNormal(pos, normal, _normalMap.rg));

    Color = _colorMap;
    Normal = vec4(n_enc, pos.x, pos.y, pos.z);    //TODO: Perturb normals
    Specular = vec4(_specularMap.rgb, 1);
}