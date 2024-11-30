#pragma once

#include <glm/vec3.hpp>
#include <glm/vec4.hpp>
#include <glm/matrix.hpp>

#include "../framework/camera.h"
#include "mesh.h"
#include "texture.h"
#include "shader.h"

#include "../libraries/easyVDB/src/bbox.h"
#include "../libraries/easyVDB/src/openvdbReader.h"

class Material {
public:

	Shader* shader = NULL;
	Texture* texture = NULL;
	glm::vec4 color;

	virtual void setUniforms(Camera* camera, glm::mat4 model) = 0;
	virtual void render(Mesh* mesh, glm::mat4 model, Camera* camera) = 0;
	virtual void renderInMenu() = 0;
};

class FlatMaterial : public Material {
public:

	FlatMaterial(glm::vec4 color = glm::vec4(1.f));
	~FlatMaterial();

	void setUniforms(Camera* camera, glm::mat4 model);
	void render(Mesh* mesh, glm::mat4 model, Camera* camera);
	void renderInMenu();
};

class WireframeMaterial : public FlatMaterial {
public:

	WireframeMaterial();
	~WireframeMaterial();

	void render(Mesh* mesh, glm::mat4 model, Camera* camera);
};

class StandardMaterial : public Material {
public:

	bool first_pass = false;

	bool show_normals = false;
	Shader* base_shader = NULL;
	Shader* normal_shader = NULL;

	StandardMaterial(glm::vec4 color = glm::vec4(1.f));
	~StandardMaterial();

	void setUniforms(Camera* camera, glm::mat4 model);
	void render(Mesh* mesh, glm::mat4 model, Camera* camera);
	void renderInMenu();

	//Lab 4

	void loadVDB(std::string file_path);

	void estimate3DTexture(easyVDB::OpenVDBReader* vdbReader);
};

class VolumeMaterial : public StandardMaterial {
public:

	int current_shader = 5;

	Shader* abs_hom_shader = NULL;
	Shader* abs_het_shader = NULL;
	Shader* emi_abs_shader = NULL;

	Shader* rabbit_shader = NULL;

	float absorption_coef = 2.0f;
	float step_length = 0.05f;
	float noise_scale = 2.5f;
	float noise_detail = 5.0f;

	float scattering_coef = 0.0f;


	VolumeMaterial(glm::vec4 color = glm::vec4(1.f));

	void setUniforms(Camera* camera, glm::mat4 model);

	void renderInMenu();
};

class RabbitMaterial : public StandardMaterial {
public:

	int density_type = 2;

	Shader* rabbit_shader = NULL;

	float absorption_coef = 2.0f;
	float step_length = 0.1f;
	float noise_scale = 2.5f;
	float noise_detail = 5.0f;

	float scattering_coef = 0.2f;


	RabbitMaterial(glm::vec4 color = glm::vec4(1.f));

	void setUniforms(Camera* camera, glm::mat4 model);

	void renderInMenu();


};


class IsosurfaceMaterial : public StandardMaterial {
public:

	int current_shader = 1;

	Shader* iso_shader = NULL;

	Shader* iso_light_shader = NULL;

	float step_length = 0.05f;

	float threshold = 1.0f;

	bool jittering = false;

	bool isosurface = true;

	float h = 0.0001;

	glm::vec4 ambient = glm::vec4(0.1, 0.1, 0.1, 1.0);

	glm::vec4 ks = glm::vec4(0.5, 0.5, 0.5, 1.0);

	float alpha = 1.0;


	IsosurfaceMaterial(glm::vec4 color = glm::vec4(1.f));

	void setUniforms(Camera* camera, glm::mat4 model);

	void renderInMenu();

};


