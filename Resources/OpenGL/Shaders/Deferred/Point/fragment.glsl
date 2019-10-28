layout(early_fragment_tests) in;    //Drop fragments that are blocked, to avoid doing doubled lighting calculations

// Interpolated values from the vertex shaders
in vec2 UV;
flat in int drawID;

// Ouput data
layout(location = 0) out vec4 light_intensity;
layout(location = 1) out vec4 color;

// Values that stay constant for the whole mesh.
uniform vec2 im_sz;
uniform vec3 viewPos;
layout(bindless_sampler) uniform sampler2D worldBuf;
layout(bindless_sampler) uniform sampler2D uvBuf;
layout(bindless_sampler) uniform usampler2D mID_Buf;

struct mat_t {
	uvec2 Albedo;
    uvec2 MetalRoughnessDerivative;
};

struct light_t {
    vec3 pos;
    float radius;
    vec3 color;
    float intensity;
};

layout(std430, binding = 0) buffer Materials_t {
    mat_t v[];
} Material;

layout(std430, binding = 1) buffer Lights_t {
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
    vec2 frag_uv = gl_FragCoord.xy / im_sz;
    uint obj_mID = texture(mID_Buf, frag_uv).r;

    if(obj_mID == 0)
        discard;

    //Read object properties
    vec2 obj_uv = texture(uvBuf, frag_uv).rg;
    vec2 obj_normPacked = texture(uvBuf, frag_uv).ba;
    vec3 obj_norm = decode(obj_normPacked);
    vec3 obj_wPos = texture(worldBuf, frag_uv).xyz;
    vec4 obj_albedo = texture(sampler2D(Material.v[obj_mID - 1].Albedo), obj_uv);
    vec4 obj_m_r_d = texture(sampler2D(Material.v[obj_mID - 1].MetalRoughnessDerivative), obj_uv);

    //Read light properties
    vec3 l_pos = Light.v[drawID].pos;
    float l_rad = Light.v[drawID].radius;
    vec3 l_color = Light.v[drawID].color;
    float l_inten = Light.v[drawID].intensity;

    //Compute lighting
    vec3 l_dir = normalize(l_pos - obj_wPos);
    vec3 view_dir = normalize(viewPos - obj_wPos);
    vec3 l_half = normalize(l_dir + view_dir);
    float dist = length(l_pos - obj_wPos);
        
    float NdL = min(max(dot(obj_norm, l_dir), 0), 1);
    float NdH = min(max(dot(obj_norm, l_half), 0), 1);
    float NdV = min(max(dot(obj_norm, view_dir), 0), 1);
    float VdH = min(max(dot(view_dir, l_half), 0), 1);
    float falloff = l_inten / (dist * dist);

    float k = obj_m_r_d.y * obj_m_r_d.y * sqrt(2.0f / PI); //Compute scaled roughness

    //float fresnel = float(obj_m_r_d.x > 0.5f) + (1 - float(obj_m_r_d.x > 0.5f)) * pow(1 - VdH, 5);
    float fresnel = obj_m_r_d.x + (1 - obj_m_r_d.x) * pow(1 - VdH, 5);
    float distribution = D_GGx(NdH, obj_m_r_d.y * obj_m_r_d.y);
    float geometry = G1_schlick(NdL, k) * G1_schlick(NdV, k);

    float specular = distribution * geometry * fresnel * 0.25f / max(NdV, 0.001f);
    float diffuse = (1 - fresnel) / PI * NdL;

	color = vec4(obj_albedo.rgb * l_color, 1);
    light_intensity = vec4(falloff, specular, diffuse, 1);

    //color.rgb *= 16;
    //vec3 x = max(vec3(0),color.rgb-0.004);
    //color.rgb = (x*(6.2*x+.5))/(x*(6.2*x+1.7)+0.06);
    //color.rgb = pow(color.rgb, vec3(1.0/2.2)); 
}