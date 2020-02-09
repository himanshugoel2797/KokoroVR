// Output data ; will be interpolated for each fragment.
out vec2 UV;

void main(){
	const vec4 verts = vec4(3, -1, -1, 3);
	const vec4 uvs = vec4(2, 0, 0, 2);
	gl_Position = vec4(verts[(gl_VertexID + 1) % 4], verts[(gl_VertexID + 2) % 4], 0.5f, 1);
	UV = vec2(uvs[(gl_VertexID + 1) % 4], uvs[(gl_VertexID + 2) % 4]);
}