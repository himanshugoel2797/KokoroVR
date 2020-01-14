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
    vec4 _colorMap = texture(sampler2D(Object.v[drawID].c), UV);
    
    float n_enc = encode(normal);

    Color = _colorMap;
    Normal = vec4(n_enc, pos.x, pos.y, pos.z);    //TODO: Perturb normals
    Specular = vec4(0, 0, 0, 1);
}