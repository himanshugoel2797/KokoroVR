//copy depth buffer into hi-z top
layout(local_size_x = 8, local_size_y = 8) in;

layout(std140, binding = 0) uniform GlobalParams_t {
	mat4 proj[EYECOUNT];
	mat4 view[EYECOUNT];
	mat4 vp[EYECOUNT];
	mat4 ivp[EYECOUNT];
	mat4 prev_view[EYECOUNT];
	mat4 prev_vp[EYECOUNT];
	mat4 prev_ivp[EYECOUNT];
	uvec4 infoBindings[EYECOUNT];
	uvec4 depthBindings[EYECOUNT];
	vec4 prev_eyePos;
	vec4 prev_eyeUp;
	vec4 prev_eyeDir;
	vec4 eyePos;
	vec4 eyeUp;
	vec4 eyeDir;
} GlobalParams;

layout(std140, binding = 1) uniform HiZLayers_t {
    uvec4 sdpp;
    uvec4 mips[EYECOUNT][MIP_COUNT];
} HiZLayers;

uniform int eyeIdx;

void main(){
    //position = gl_GlobalInvocationID
    sampler2D depthBuf = sampler2D(GlobalParams.depthBindings[eyeIdx].xy);
    image2D hiZtop = image2D(HiZLayers.mips[eyeIdx][0].xy);

    ivec2 p = ivec2(gl_GlobalInvocationID.xy);
    float depth = texelFetch(depthBuf, p, 0).x;
    imageStore(hiZtop, p, vec4(depth));
}