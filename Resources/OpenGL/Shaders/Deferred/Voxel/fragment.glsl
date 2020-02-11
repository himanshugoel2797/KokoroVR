// Interpolated values from the vertex shaders
//in vec3 normal;
in vec3 pos;
flat in uint vox_v;
flat in uint vox_idx;

// Ouput data
layout(location = 0) out vec4 Info;

layout(std140, binding = 0) uniform GlobalParams_t {
	mat4 proj[EYECOUNT];
	mat4 view[EYECOUNT];
	mat4 vp[EYECOUNT];
	uvec4 infoBindings[EYECOUNT];
	uvec4 depthBindings[EYECOUNT];
	vec4 eyePos;
	vec4 eyeUp;
	vec4 eyeDir;
} GlobalParams;

struct block_info_t {
	vec4 o;
};

layout(std430, binding = 1) readonly buffer BlockInfos_t {
    block_info_t v[];
} BlockInfo;

//pack into a single 32-bit float, for the gbuffer
float encode (vec3 n)
{
    return uintBitsToFloat(packSnorm4x8(vec4(n, 1)));
}

void main(){
    //Read object properties
    vec3 dx_pos = dFdx(pos);
    vec3 dy_pos = dFdy(pos);

    vec3 normal = normalize(cross(dx_pos, dy_pos));
    float n_enc = encode(normal);

    Info = vec4(round(pos + GlobalParams.eyePos.xyz - BlockInfo.v[vox_idx].o.xyz), 1);
}