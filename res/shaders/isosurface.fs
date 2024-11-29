#version 450 core

in vec3 v_position;
in vec3 v_world_position;
in vec3 v_normal;

uniform vec3 u_camera_position;

uniform vec4 u_ambient_light;
uniform vec4 u_background_light;
uniform vec4 u_color;

uniform mat4 u_model;

uniform float u_step_length;

uniform sampler3D u_texture;  // 3D texture for density data

uniform float u_threshold;

uniform bool u_use_jittering;

uniform bool u_use_isosurface;

out vec4 FragColor;

// ---------------------------------------------------------------------------------------------------//

float random (vec2 st) {
    return fract(sin(dot(st.xy, vec2(12.9898,78.233)))*43758.5453123);
}

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
    float sum = 0.0;
    vec4 final_color = u_background_light;
    float density;

    float ray_init_pos = tNear;
    
    if (u_use_jittering) {

        position += random(gl_FragCoord.xy) * u_step_length * rayDir;

    } 

    // Ray-marching loop for emission-absorption
    for (float t = ray_init_pos; t < tFar; t += u_step_length) {

        vec3 localPosition = (inverse(u_model) * vec4(position, 1.0)).xyz;
        vec3 texCoords = (localPosition + vec3(1.0)) * 0.5; // Map to [0, 1] range
        density = texture(u_texture, texCoords).x;
        
        sum += density;

        if (u_use_isosurface) {

            if (sum > u_threshold) {

            final_color = u_color;
            break;
            }
        }

        // Advance position along the ray
        position += rayDir * u_step_length;

    }

    // Final color calculation, combining accumulated radiance and background light

    if (u_use_isosurface) {
        FragColor = final_color;
    }
    else {
        FragColor = u_background_light * exp(-sum * 2 * u_step_length);
    }
}