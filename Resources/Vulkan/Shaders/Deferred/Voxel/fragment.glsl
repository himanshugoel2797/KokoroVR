layout(location = 0) in vec3 pos;
layout(location = 1) flat in uint vox_v;
layout(location = 2) flat in uint vox_idx;

// Ouput data
layout(location = 0) out vec4 Info;
layout(location = 1) out vec4 Info2;

layout(std140, set = 0, binding = 0) uniform GlobalParams_t {
	mat4 proj;
	mat4 view;
	mat4 vp;
	mat4 ivp;
	mat4 prev_view;
	mat4 prev_vp;
	mat4 prev_ivp;
	vec4 prev_eyePos;
	vec4 prev_eyeUp;
	vec4 prev_eyeDir;
	vec4 eyePos;
	vec4 eyeUp;
	vec4 eyeDir;
} GlobalParams;

struct block_info_t {
	vec4 o;
};

layout(std430, set = 0, binding = 1) readonly buffer BlockInfos_t {
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

    Info = vec4(normal * 0.5f + 0.5f, 1);//vec4(pos / 32.0f /*+ GlobalParams.eyePos.xyz*/, 0);
	Info2 = vec4(normal * 0.5f + 0.5f, 0);

	//gl_FragDepth = 0.00000001f;
}