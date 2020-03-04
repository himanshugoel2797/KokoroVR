layout(location = 0) out vec2 uv;

layout(std140, set = 0, binding = 0) uniform GlobalInfo_t{
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
} GlobalInfo;

vec2 positions[3] = vec2[](
    vec2(0.0, -0.5),
    vec2(0.5, 0.5),
    vec2(-0.5, 0.5)
);

void main() {
    float x = -1.0f + float((gl_VertexIndex & 1) << 2);
	float y = -1.0f + float((gl_VertexIndex & 2) << 1);
    
    gl_Position = GlobalInfo.vp * vec4(x, y, 0.5f, 1.0f);
    uv = (vec2(x, y) + vec2(1)) * 0.5f;
}