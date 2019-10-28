// Input vertex data, different for all executions of this shader.
layout(location = 0) in vec3 position;
layout(location = 1) in vec2 vertexUV;

// Output data ; will be interpolated for each fragment.
out vec2 UV;
flat out int drawID;

void main(){
	gl_Position = vec4(position, 1);
	UV = vertexUV;
	drawID = gl_BaseInstance + gl_InstanceID;
}