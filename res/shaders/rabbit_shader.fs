#version 450 core

in vec3 v_position;
in vec3 v_world_position;
in vec3 v_normal;

uniform vec3 u_camera_position;

uniform vec4 u_ambient_light;
uniform vec4 u_background_light;
uniform vec4 u_color;

uniform mat4 u_model;

uniform float u_abs_coef;
uniform float u_step_length;
uniform float u_noise_scale;
uniform float u_noise_detail;

uniform sampler3D u_texture;  // 3D texture for density data
//uniform bool u_use_volume_data;      // Flag to use 3D texture data or procedural noise

out vec4 FragColor;

void main() {

    vec3 rayOrigin = u_camera_position;
    vec3 rayDir = normalize(v_world_position - u_camera_position);

    vec3 boxMin = vec3(-1);
    vec3 boxMax = vec3(1);

    // Intersect AABB
    vec3 tMin = (boxMin - rayOrigin) / rayDir;
    vec3 tMax = (boxMax - rayOrigin) / rayDir;
    vec3 t1 = min(tMin, tMax);
    vec3 t2 = max(tMin, tMax);
    float tNear = max(max(t1.x, t1.y), t1.z);
    float tFar = min(min(t2.x, t2.y), t2.z);

    vec3 position = rayOrigin + tNear * rayDir;
    vec4 sum = vec4(0.0);
    float thickness = 0.0;
    float density = 0.0;

    // Ray-marching loop for emission-absorption
    for (float t = tNear; t < tFar; t += u_step_length) {

        // Sample density from the 3D volume texture

        vec3 localPosition = (inverse(u_model) * vec4(position, 1.0)).xyz;

        vec3 texCoords = (localPosition + vec3(1.0)) * 0.5; // Map to [0, 1] range
        density = texture(u_texture, texCoords).x;

        float absorption = density * u_abs_coef;
        float transmittance = exp(-absorption * u_step_length);

        // Calculate radiance and accumulate it
        vec4 radiance = absorption * u_color;
        sum += transmittance * radiance * u_step_length;

        // Advance position along the ray
        position += rayDir * u_step_length;

        // Accumulate optical thickness for background blending
        thickness += absorption * u_step_length;
    }

    // Final color calculation, combining accumulated radiance and background light
    FragColor = sum + u_background_light * exp(-thickness);
}