// Input vertex data, different for all executions of this shader.
layout(location = 0) in vec3 vs_pos;
layout(location = 1) in vec2 vs_uv;
layout(location = 2) in vec2 vs_normal;

// Output data ; will be interpolated for each fragment.
out vec2 UV;
out vec3 normal;
out vec3 pos;
flat out int drawID;

// Values that stay constant for the whole mesh.
uniform mat4 View;
uniform mat4 Proj;

layout(std430, binding = 1) buffer Transforms_t {
    mat4 w[];
} Transforms;

void main(){

	// Output position of the vertex, in clip space : MVP * position
    vec4 wPos = Transforms.w[gl_BaseInstance + gl_InstanceID ] * vec4(vs_pos.x, vs_pos.y, vs_pos.z, 1);
	gl_Position =  Proj * View * wPos;

	UV = vs_uv;
	vec2 n = vs_normal / 100.0f * PI/180.0f;
	normal.x = cos(n.x) * sin(n.y);
	normal.y = sin(n.x) * sin(n.y);
	normal.z = cos(n.y);
    pos = wPos.xyz;
	drawID = gl_BaseInstance + gl_InstanceID;
}