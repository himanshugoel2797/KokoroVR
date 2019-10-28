layout(early_fragment_tests) in;    //Drop fragments that are blocked, to avoid doing doubled lighting calculations

// Interpolated values from the vertex shaders
in vec2 UV;
flat in int drawID;

// Ouput data
layout(location = 0) out vec4 light;

// Values that stay constant for the whole mesh.
uniform vec3 EyePos;

layout(bindless_sampler) uniform sampler2D ColorMap;    //R:G:B:Roughness
layout(bindless_sampler) uniform sampler2D NormalMap;   //NX|NY:WX:WY:WZ
layout(bindless_sampler) uniform sampler2D SpecularMap; //SR:SG:SB

struct light_t {
    vec3 direction;
    float intensity;
    vec3 color;
};

layout(std430, binding = 0) buffer Lights_t {
    light_t v[];
} Light;

vec3 decode (vec2 enc)
{
    vec4 nn = vec4(enc, 0, 0)*vec4(2,2,0,0) + vec4(-1,-1,1,-1);
    float l = dot(nn.xyz,-nn.xyw);
    nn.z = l;
    nn.xy *= sqrt(l);
    return nn.xyz * 2 + vec3(0,0,-1);
}

float G1_schlick(float NdV, float k){
    return NdV / (NdV * (1 - k) + k);
}

float D_GGx(float NdH, float a){
    float a2 = a * a;
    float t0 = NdH * NdH * (a2 - 1) + 1;
    return (a2) / (PI * t0 * t0);
}

void main(){
    //Read object properties
    vec4 _colorMap = texture(ColorMap, UV);
    vec4 _normalMap = texture(NormalMap, UV);
    vec4 _specularMap = texture(SpecularMap, UV);

    vec3 obj_norm = decode(vec2(_normalMap.r, _specularMap.w));
    vec3 obj_wPos = _normalMap.yzw;
    vec3 obj_albedo = _colorMap.rgb;
    vec3 obj_specular = _specularMap.rgb;
    float roughness = _colorMap.a;

    //Read light properties
    vec3 l_color = Light.v[drawID].color;
    float l_inten = Light.v[drawID].intensity;
    vec3 l_dir = Light.v[drawID].direction;

    //Compute lighting
    vec3 view_dir = normalize(EyePos - obj_wPos);
    vec3 l_half = normalize(l_dir + view_dir);
        
    float NdL = min(max(dot(obj_norm, l_dir), 0), 1);
    float NdH = min(max(dot(obj_norm, l_half), 0), 1);
    float NdV = min(max(dot(obj_norm, view_dir), 0), 1);
    float VdH = min(max(dot(view_dir, l_half), 0), 1);

    float k = roughness * roughness * sqrt(2.0f / PI); //Compute scaled roughness

    vec3 fresnel = obj_specular + (1 - obj_specular) * pow(1 - VdH, 5);
    float distribution = D_GGx(NdH, roughness * roughness);
    float geometry = G1_schlick(NdL, k) * G1_schlick(NdV, k);

    vec3 specular = distribution * geometry * fresnel * 0.25f / max(NdV, 0.001f);
    vec3 diffuse = (1 - fresnel) * obj_albedo / PI;

    light = vec4((specular + diffuse) * l_color * l_inten * NdL , 1);
}