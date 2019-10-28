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

vec2 encode (vec3 n)
{
    vec2 enc = normalize(n.xy) * (sqrt(-n.z*0.5+0.5));
    enc = enc*0.5+0.5;
    return enc;
}

void main(){
    //Read object properties
    vec4 _colorMap = texture(sampler2D(Object.v[drawID].c), UV);
    
    vec2 n_enc = encode(normal);

    Color = _colorMap;
    Normal = vec4(n_enc.x, pos.x, pos.y, pos.z);    //TODO: Perturb normals
    Specular = vec4(0, 0, 0, n_enc.y);
}