// Interpolated values from the vertex shaders
//in vec3 normal;
in vec3 pos;
flat in uint vox_v;
flat in uint vox_idx;

// Ouput data
layout(location = 0) out vec4 Color;
layout(location = 1) out vec4 Normal;
layout(location = 2) out vec4 Specular;

uniform vec3 eyePos;

struct obj_t {
    vec4 c;
    vec4 s;
};

layout(std430, binding = 0) buffer Objects_t {
    obj_t v[];
} Object;

//pack into a single 32-bit float, for the gbuffer
float encode (vec3 n)
{
    return uintBitsToFloat(packSnorm4x8(vec4(n, 1)));
}

void main(){
    //Read object properties
    vec4 _colorMap = Object.v[vox_v].c;
    vec4 _specMap = Object.v[vox_v].s;

    vec3 dx_pos = dFdx(pos);
    vec3 dy_pos = dFdy(pos);

    vec3 normal = normalize(cross(dx_pos, dy_pos));
    float n_enc = encode(normal);

    Color = _colorMap;
    Normal = vec4(n_enc, pos.x + eyePos.x, pos.y + eyePos.y, pos.z + eyePos.z);
    Specular = vec4(_specMap.x, _specMap.y, _specMap.z, 1);
}