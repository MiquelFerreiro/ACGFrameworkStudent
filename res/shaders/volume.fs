#version 450 core

in vec3 v_position;
in vec3 v_world_position;
in vec3 v_normal;

uniform vec3 u_camera_position;

uniform vec4 u_color;
uniform vec4 u_ambient_light;

uniform vec3 u_light_position;
uniform vec4 u_light_color;
uniform float u_light_intensity;
uniform float u_light_shininess;

uniform float u_abs_coef;
uniform vec4 u_background_light;

out vec4 FragColor;

void main()
{

	vec3 rayOrigin = u_camera_position;
	vec3 rayDir = normalize(v_world_position - u_camera_position);

	vec3 boxMin = vec3(-1);
	vec3 boxMax = vec3(1);

	//funcion intersectAABB
	vec3 tMin = (boxMin - rayOrigin) / rayDir;
    vec3 tMax = (boxMax - rayOrigin) / rayDir;
    vec3 t1 = min(tMin, tMax);
    vec3 t2 = max(tMin, tMax);
    float tNear = max(max(t1.x, t1.y), t1.z);
    float tFar = min(min(t2.x, t2.y), t2.z);

	float transmittance = exp(-u_abs_coef*(-tNear + tFar));

	FragColor = u_background_light * transmittance;
}
