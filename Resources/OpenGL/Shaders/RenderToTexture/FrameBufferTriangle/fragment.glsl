
// Interpolated values from the vertex shaders
in vec2 UV;

// Ouput data
layout(location = 0) out vec4 color;

// Values that stay constant for the whole mesh.
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

void main(){
	color = texture(sampler2D(GlobalParams.infoBindings[0].xy), UV);
	color.a = 1;
}